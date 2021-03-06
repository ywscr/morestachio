﻿using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Morestachio.Framework.Expression.Visitors;

namespace Morestachio.Framework.Expression
{
	/// <summary>
	///		Defines an Argument used within a formatter
	/// </summary>
	public class ExpressionArgument : IEquatable<ExpressionArgument>,IMorestachioExpression
	{
		internal ExpressionArgument()
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="location"></param>
		public ExpressionArgument(CharacterLocation location)
		{
			Location = location;
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected ExpressionArgument(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString(nameof(Name));
			MorestachioExpression = info.GetValue(nameof(MorestachioExpression), typeof(IMorestachioExpression)) as IMorestachioExpression;
			Location = CharacterLocation.FromFormatString(info.GetString(nameof(Location)));
		}

		/// <summary>
		///		The name of the Argument
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///		The value of the Argument
		/// </summary>
		public IMorestachioExpression MorestachioExpression { get; set; }

		/// <summary>
		///		The Location within the Template
		/// </summary>
		public CharacterLocation Location { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="contextObject"></param>
		/// <param name="scopeData"></param>
		/// <returns></returns>
		public async Task<ContextObject> GetValue(ContextObject contextObject, ScopeData scopeData)
		{
			return await MorestachioExpression.GetValue(contextObject, scopeData);
		}

		/// <inheritdoc />
		public void Accept(IMorestachioExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <inheritdoc />
		public XmlSchema GetSchema()
		{
			throw new NotImplementedException();
		}
		
		/// <inheritdoc />
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(Name), Name);
			info.AddValue(nameof(Location), Location.ToFormatString());
			info.AddValue(nameof(MorestachioExpression), MorestachioExpression);
		}
		
		/// <inheritdoc />
		public void ReadXml(XmlReader reader)
		{
			Location = CharacterLocation.FromFormatString(reader.GetAttribute(nameof(Location)));
			Name = reader.GetAttribute(nameof(Name));
			reader.ReadStartElement();

			var expSubtree = reader.ReadSubtree();
			expSubtree.Read();
			MorestachioExpression = expSubtree.ParseExpressionFromKind();
		}
		
		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			if (Name != null)
			{
				writer.WriteAttributeString(nameof(Name), Name);
			}
			writer.WriteAttributeString(nameof(Location), Location.ToFormatString());
			writer.WriteExpressionToXml(MorestachioExpression);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var visitor = new ToParsableStringExpressionVisitor();
			Accept(visitor);
			return visitor.StringBuilder.ToString();
		}

		/// <inheritdoc />
		public bool Equals(ExpressionArgument other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Name == other.Name && MorestachioExpression.Equals(other.MorestachioExpression) && Location.Equals(other.Location);
		}

		/// <inheritdoc />
		public bool Equals(IMorestachioExpression other)
		{
			return Equals((object)other);
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

			return Equals((ExpressionArgument) obj);
		}
		
		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (MorestachioExpression != null ? MorestachioExpression.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}