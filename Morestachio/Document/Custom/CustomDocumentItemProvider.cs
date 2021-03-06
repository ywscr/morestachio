﻿using System;
using System.Collections.Generic;
using System.Text;
using Morestachio.Document.Contracts;
using Morestachio.Framework;
using Morestachio.Framework.Expression;
using Morestachio.ParserErrors;

namespace Morestachio.Document.Custom
{
	/// <summary>
	///		Allows the injection of a custom DocumentItem 
	/// </summary>
	public abstract class CustomDocumentItemProvider
	{
		public CustomDocumentItemProvider()
		{
			ScopeStack = new Stack<Tuple<string, int>>();
		}

		/// <summary>
		///		A Custom stack that keeps track of enclosing tokens such as #IF and /IF
		/// </summary>
		public Stack<Tuple<string, int>> ScopeStack { get; }

		/// <summary>
		///		Should check if the token contains this partial token. If returns true further actions will happen.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public abstract bool ShouldTokenize(string token);

		/// <summary>
		///		An helper object that will be given to the Tokenize method
		/// </summary>
		public class TokenInfo
		{

			internal TokenInfo(string token,
				TokenzierContext context, 
				Stack<Tuple<string, int>> scopeStack)
			{
				TokenizerContext = context;
				ScopeStack = scopeStack;
				Token = token;
				Errors = new List<IMorestachioError>();
			}
			
			/// <summary>
			///		Provides the current context you document item is created in
			/// </summary>
			public TokenzierContext TokenizerContext { get; set; }

			/// <summary>
			///		The global scope stack
			/// </summary>
			public Stack<Tuple<string, int>> ScopeStack { get; }

			/// <summary>
			///		The obtained Token. This is the Full text token
			/// </summary>
			public string Token { get; }

			/// <summary>
			///		Can be filled to return errors that occured in the formatting process
			/// </summary>
			public ICollection<IMorestachioError> Errors { get; }
		}

		/// <summary>
		///		Should return any kind of token Pair that encapsulates the value for the DocumentItem
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public abstract IEnumerable<TokenPair> Tokenize(TokenInfo token, ParserOptions options);

		/// <summary>
		///		Should return True if the Token is produced by this provider and should be parsed with this provider
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public abstract bool ShouldParse(TokenPair token, ParserOptions options);

		/// <summary>
		///		Should return an document item that will be invoked when parsing the Template
		/// </summary>
		/// <param name="token"></param>
		/// <param name="options"></param>
		/// <param name="buildStack"></param>
		/// <returns></returns>
		public abstract IDocumentItem Parse(TokenPair token, ParserOptions options, Stack<DocumentScope> buildStack);
	}
}
