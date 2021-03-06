﻿using System.Runtime.CompilerServices;
using Morestachio.Document;
using Morestachio.Document.Contracts;
using Morestachio.Formatter;
using Morestachio.Framework.Expression;
using Morestachio.ParserErrors;

#region

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Morestachio.Framework;

#endregion

namespace Morestachio
{
	/// <summary>
	///     The main entry point for this library. Use the static "Parse" methods to create template functions.
	///     Functions are safe for reuse, so you may parse and cache the resulting function.
	/// </summary>
	public static class Parser
	{
		/// <summary>
		///     Parses the Template with the given options
		/// </summary>
		/// <param name="parsingOptions">a set of options</param>
		/// <returns></returns>
		[ContractAnnotation("parsingOptions:null => halt")]
		[NotNull]
		[MustUseReturnValue("Use return value to create templates. Reuse return value if possible.")]
		[Pure]
		public static MorestachioDocumentInfo ParseWithOptions([NotNull] ParserOptions parsingOptions)
		{
			if (parsingOptions == null)
			{
				throw new ArgumentNullException(nameof(parsingOptions));
			}

			if (parsingOptions.SourceFactory == null)
			{
				throw new ArgumentNullException(nameof(parsingOptions), "The given Stream is null");
			}

			var profiler = new PerformanceProfiler(parsingOptions.ProfileExecution);
			Queue<TokenPair> tokens;
			TokenzierContext context;
			using (profiler.Begin("Tokenize"))
			{
				tokens = new Queue<TokenPair>(Tokenizer.Tokenize(parsingOptions, profiler, out context));
			}

			//if there are any errors do not parse the template
			var documentInfo = new MorestachioDocumentInfo(parsingOptions,
				context.Errors.Any() ? null : Parse(tokens, parsingOptions), context.Errors);
			documentInfo.Profiler = profiler;
			return documentInfo;
		}

		/// <summary>
		///     Parses the Tokens into a Document.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <returns></returns>
		internal static IDocumentItem Parse(Queue<TokenPair> tokens, ParserOptions options)
		{
			var buildStack = new Stack<DocumentScope>();
			//instead of recursive calling the parse function we stack the current document 
			buildStack.Push(new DocumentScope(new MorestachioDocument()));

			while (tokens.Any())
			{
				var currentToken = tokens.Dequeue();
				var currentDocumentItem = buildStack.Peek(); //get the latest document

				if (currentToken.Type.Equals(TokenType.Comment))
				{
					//just ignore this part and print nothing
				}
				else if (currentToken.Type.Equals(TokenType.Content))
				{
					currentDocumentItem.Document.Add(new ContentDocumentItem(currentToken.Value)
					{
						ExpressionStart = currentToken.TokenLocation
					});
				}
				else if (currentToken.Type.Equals(TokenType.If))
				{
					var nestedDocument = new IfExpressionScopeDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.IfNot))
				{
					var nestedDocument = new IfNotExpressionScopeDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.Else))
				{
					var nestedDocument = new ElseExpressionScopeDocumentItem()
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.CollectionOpen))
				{
					var nestedDocument = new EachDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.WhileLoopOpen))
				{
					var nestedDocument = new WhileLoopDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.DoLoopOpen))
				{
					var nestedDocument = new DoLoopDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.ElementOpen))
				{
					var nestedDocument = new ExpressionScopeDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(nestedDocument);
				}
				else if (currentToken.Type.Equals(TokenType.InvertedElementOpen))
				{
					var invertedScope = new InvertedExpressionScopeDocumentItem(currentToken.MorestachioExpression)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					buildStack.Push(new DocumentScope(invertedScope));
					currentDocumentItem.Document.Add(invertedScope);
				}
				else if (currentToken.Type.Equals(TokenType.CollectionClose)
						|| currentToken.Type.Equals(TokenType.ElementClose)
						|| currentToken.Type.Equals(TokenType.IfClose)
						|| currentToken.Type.Equals(TokenType.ElseClose)
						|| currentToken.Type.Equals(TokenType.WhileLoopClose)
						|| currentToken.Type.Equals(TokenType.DoLoopClose))
				{
					if (buildStack.Peek().HasAlias) //are we in a alias then remove it
					{
						currentDocumentItem.Document.Add(new RemoveAliasDocumentItem(buildStack.Peek().AliasName));
						buildStack.Pop();
					}
					// remove the last document from the stack and go back to the parents
					buildStack.Pop();
				}
				else if (currentToken.Type.Equals(TokenType.EscapedSingleValue) ||
						currentToken.Type.Equals(TokenType.UnescapedSingleValue))
				{
					currentDocumentItem.Document.Add(new PathDocumentItem(currentToken.MorestachioExpression,
							currentToken.Type.Equals(TokenType.EscapedSingleValue))
					{
						ExpressionStart = currentToken.TokenLocation
					});
				}
				else if (currentToken.Type.Equals(TokenType.PartialDeclarationOpen))
				{
					// currently same named partials will override each other
					// to allow recursive calls of partials we first have to declare the partial and then load it as we would parse
					// -the partial as a whole and then add it to the list would lead to unknown calls of partials inside the partial
					var nestedDocument = new MorestachioDocument();
					buildStack.Push(new DocumentScope(nestedDocument));
					currentDocumentItem.Document.Add(new PartialDocumentItem(currentToken.Value, nestedDocument)
					{
						ExpressionStart = currentToken.TokenLocation
					});
				}
				else if (currentToken.Type.Equals(TokenType.PartialDeclarationClose))
				{
					currentDocumentItem.Document.Add(new RenderPartialDoneDocumentItem(currentToken.Value)
					{
						ExpressionStart = currentToken.TokenLocation
					});
					buildStack.Pop();
				}
				else if (currentToken.Type.Equals(TokenType.RenderPartial))
				{
					currentDocumentItem.Document.Add(new RenderPartialDocumentItem(currentToken.Value)
					{
						ExpressionStart = currentToken.TokenLocation
					});
				}
				else if (currentToken.Type.Equals(TokenType.Alias))
				{
					var aliasDocumentItem = new AliasDocumentItem(currentToken.Value)
					{
						ExpressionStart = currentToken.TokenLocation
					};
					currentDocumentItem.Document.Add(aliasDocumentItem);
					buildStack.Push(new DocumentScope(aliasDocumentItem, currentToken.Value));
				}
				else if (currentToken.Type.Equals(TokenType.VariableDeclaration))
				{
					var evaluateVariableDocumentItem = new EvaluateVariableDocumentItem(currentToken.Value, currentToken.MorestachioExpression);
					currentDocumentItem.Document.Add(evaluateVariableDocumentItem);
				}
				else
				{
					var customDocumentItemProvider =
						options.CustomDocumentItemProviders.FirstOrDefault(e => e.ShouldParse(currentToken, options));
					if (customDocumentItemProvider != null)
					{
						var documentItem = customDocumentItemProvider.Parse(currentToken, options, buildStack);
						currentDocumentItem.Document.Add(documentItem);
					}
				}
			}

			if (buildStack.Count != 1)
			{
				//var invalidScopedElements = buildStack
				//throw new MorestachioSyntaxError(new Tokenizer.CharacterLocation(){Character = }, );
				throw new InvalidOperationException(
					"There is an Error with the Parser. The Parser still contains unscoped builds: " +
					buildStack.Select(e => e.Document.Kind).Aggregate((e, f) => e + ", " + f));
			}

			return buildStack.Pop().Document;
		}
	}
}