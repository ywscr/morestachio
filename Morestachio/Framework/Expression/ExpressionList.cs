﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Morestachio.Framework.Expression.Visitors;

namespace Morestachio.Framework.Expression
{
	/// <summary>
	///		Defines a list of Expressions 
	/// </summary>
	public class MorestachioExpressionList : IMorestachioExpression
	{
		/// <summary>
		///		The list of Expressions
		/// </summary>
		public IList<IMorestachioExpression> Expressions { get; private set; }

		internal MorestachioExpressionList()
		{

		}

		/// <summary>
		///	
		/// </summary>
		/// <param name="expressions"></param>
		public MorestachioExpressionList(IList<IMorestachioExpression> expressions)
		{
			Expressions = expressions;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected MorestachioExpressionList(SerializationInfo info, StreamingContext context)
		{
			Location = CharacterLocation.FromFormatString(info.GetString(nameof(Location)));
			Expressions = (IMorestachioExpression[])info.GetValue(nameof(Expressions), typeof(IMorestachioExpression[]));
		}
		
		/// <inheritdoc />
		public CharacterLocation Location { get; set; }
		/// <inheritdoc />
		public async Task<ContextObject> GetValue(ContextObject contextObject, ScopeData scopeData)
		{
			contextObject = contextObject.CloneForEdit();
			foreach (var expression in Expressions)
			{
				contextObject = await expression.GetValue(contextObject, scopeData);
			}

			return contextObject;
		}
		
		/// <inheritdoc />
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(Location), Location.ToFormatString());
			info.AddValue(nameof(Expressions), Expressions.ToArray());
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
			var expression = new List<IMorestachioExpression>();
			while (reader.NodeType != XmlNodeType.EndElement)
			{
				var childTree = reader.ReadSubtree();
				childTree.Read();
				expression.Add(reader.ParseExpressionFromKind());
				reader.Skip();
			}

			Expressions = expression.ToArray();
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Location), Location.ToFormatString());
			foreach (var expression in Expressions)
			{
				writer.WriteExpressionToXml(expression);
			}
		}
		
		/// <inheritdoc />
		public override string ToString()
		{
			var visitor = new ToParsableStringExpressionVisitor();
			Accept(visitor);
			return visitor.StringBuilder.ToString();
		}


		/// <inheritdoc />
		public void Accept(IMorestachioExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}
		
		/// <inheritdoc />
		public bool Equals(IMorestachioExpression other)
		{
			return Equals((object)other);
		}
		
		/// <inheritdoc />
		protected bool Equals(MorestachioExpressionList other)
		{
			if (!Location.Equals(other.Location))
			{
				return false;
			}

			if (other.Expressions.Count != Expressions.Count)
			{
				return false;
			}

			for (var index = 0; index < Expressions.Count; index++)
			{
				var expression = Expressions[index];
				var otherExp = other.Expressions[index];
				if (!expression.Equals(otherExp))
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

			return Equals((MorestachioExpressionList) obj);
		}
		
		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Expressions != null ? Expressions.GetHashCode() : 0) * 397) ^ (Location != null ? Location.GetHashCode() : 0);
			}
		}

		protected internal void Add(IMorestachioExpression currentScopeValue)
		{
			Expressions.Add(currentScopeValue);
		}
	}
}