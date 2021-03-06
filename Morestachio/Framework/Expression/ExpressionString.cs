﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Morestachio.Framework.Expression.Visitors;
using Morestachio.ParserErrors;

namespace Morestachio.Framework.Expression
{
	/// <summary>
	///		A constant part of an string
	/// </summary>
	public class ExpressionStringConstPart : IEquatable<ExpressionStringConstPart>
	{
		/// <summary>
		///		The content of the Text Part
		/// </summary>
		public string PartText { get; set; }

		/// <summary>
		///		Where in the string is this part located
		/// </summary>
		public CharacterLocation Location { get; set; }

		/// <inheritdoc />
		public bool Equals(ExpressionStringConstPart other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return PartText == other.PartText && Location.Equals(other.Location);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((ExpressionStringConstPart)obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((PartText != null ? PartText.GetHashCode() : 0) * 397) ^ (Location != null ? Location.GetHashCode() : 0);
			}
		}
	}

	/// <summary>
	///		Defines a string as or inside an expression
	/// </summary>
	public class MorestachioExpressionString : IMorestachioExpression
	{
		public MorestachioExpressionString()
		{
			StringParts = new List<ExpressionStringConstPart>();
		}

		protected MorestachioExpressionString(SerializationInfo info, StreamingContext context)
		{
			StringParts = (IList<ExpressionStringConstPart>)info.GetValue(nameof(StringParts), typeof(IList<ExpressionStringConstPart>));
			Location = CharacterLocation.FromFormatString(info.GetString(nameof(Location)));
			Delimiter = info.GetChar(nameof(Delimiter));
		}

		/// <inheritdoc />
		public XmlSchema GetSchema()
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public void ReadXml(XmlReader reader)
		{
			Location = CharacterLocation.FromFormatString(reader.GetAttribute(nameof(Location)));
			if (reader.IsEmptyElement)
			{
				return;
			}
			reader.ReadStartElement();
			while (reader.Name == nameof(ExpressionStringConstPart) && reader.NodeType != XmlNodeType.EndElement)
			{
				var constStr = new ExpressionStringConstPart();
				constStr.Location = CharacterLocation.FromFormatString(reader.GetAttribute(nameof(Location)));
				var constStrPartText = reader.ReadElementContentAsString();
				Delimiter = constStrPartText[0];
				constStr.PartText = constStrPartText.Substring(1, constStrPartText.Length - 2);
				StringParts.Add(constStr);
				reader.ReadEndElement();
			}
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Location), Location.ToFormatString());
			foreach (var expressionStringConstPart in StringParts.Where(e => !(e.PartText is null)))
			{
				writer.WriteStartElement(expressionStringConstPart.GetType().Name);
				writer.WriteAttributeString(nameof(Location), expressionStringConstPart.Location.ToFormatString());
				writer.WriteString(Delimiter + expressionStringConstPart.PartText + Delimiter);
				writer.WriteEndElement();
			}
		}

		/// <inheritdoc />
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(StringParts), StringParts);
			info.AddValue(nameof(Location), Location.ToFormatString());
			info.AddValue(nameof(Delimiter), Delimiter);
		}

		public IList<ExpressionStringConstPart> StringParts { get; set; }

		/// <inheritdoc />
		public CharacterLocation Location { get; set; }

		/// <summary>
		///		The original Delimiter
		/// </summary>
		public char Delimiter { get; set; }

		/// <inheritdoc />
		public async Task<ContextObject> GetValue(ContextObject contextObject, ScopeData scopeData)
		{
			await Task.CompletedTask;
			return contextObject.Options.CreateContextObject(".", contextObject.CancellationToken,
				string.Join("", StringParts.Select(f => f.PartText)),
				contextObject);
		}

		

		/// <summary>
		///		Parses a text into an Expression string. Must start with ether " or '
		/// </summary>
		/// <param name="text"></param>
		/// <param name="offset"></param>
		/// <param name="context"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static MorestachioExpressionString ParseFrom(string text,
			int offset,
			TokenzierContext context,
			out int index)
		{
			var result = new MorestachioExpressionString()
			{
				Location = context.CurrentLocation
			};
			var isEscapeChar = false;
			var currentPart = new ExpressionStringConstPart()
			{
				Location = context.CurrentLocation,
				PartText = string.Empty
			};
			//get the string delimiter thats ether " or '
			result.Delimiter = text[offset];
			result.StringParts.Add(currentPart);
			//skip the string delimiter
			for (index = offset + 1; index < text.Length; index++)
			{
				var c = text[index];
				if (isEscapeChar)
				{
					currentPart.PartText += c;
					if (c == result.Delimiter)
					{
						isEscapeChar = false;
					}
				}
				else
				{
					if (c == '\\')
					{
						isEscapeChar = true;
					}
					else if (c == result.Delimiter)
					{
						if (offset == 0 && index + 1 != text.Length)
						{
							context.Errors.Add(new MorestachioSyntaxError(
								context
									.CurrentLocation
									.Offset(index)
									.AddWindow(new CharacterSnippedLocation(0, index, text)),
								"", c.ToString(), "did not expect " + result.Delimiter));
							break;
						}

						break;
					}
					else
					{
						currentPart.PartText += c;
					}
				}
			}
			context.AdvanceLocation(text.Length);
			return result;
		}

		/// <inheritdoc />
		public void Accept(IMorestachioExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var visitor = new ToParsableStringExpressionVisitor();
			Accept(visitor);
			return visitor.StringBuilder.ToString();
		}

		/// <inheritdoc />
		public bool Equals(IMorestachioExpression other)
		{
			return Equals((object)other);
		}

		/// <inheritdoc />
		protected bool Equals(MorestachioExpressionString other)
		{
			if (Delimiter != other.Delimiter || !Location.Equals(other.Location))
			{
				return false;
			}

			if (StringParts.Count != other.StringParts.Count)
			{
				return false;
			}

			for (var index = 0; index < StringParts.Count; index++)
			{
				var leftStrPart = StringParts[index];
				var rightStrPart = other.StringParts[index];
				if (!leftStrPart.Equals(rightStrPart))
				{
					return false;
				}
			}

			return true;
		}
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((MorestachioExpressionString)obj);
		}
		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (StringParts != null ? StringParts.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Delimiter.GetHashCode();
				return hashCode;
			}
		}
	}
}