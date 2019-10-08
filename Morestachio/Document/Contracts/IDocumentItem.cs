﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Morestachio.Framework;

namespace Morestachio.Document.Contracts
{
	internal interface IMorestachioDocument : IDocumentItem, IXmlSerializable, ISerializable
	{

	}

	/// <summary>
	///		Defines a Part in the Template that can be processed
	/// </summary>
	public interface IDocumentItem
	{
		/// <summary>
		///		Renders its Value into the <see cref="outputStream"/>.
		///		If there are any Document items that should be executed directly after they should be returned		
		/// </summary>
		/// <param name="outputStream">The output stream.</param>
		/// <param name="context">The context.</param>
		/// <param name="scopeData">The scope data.</param>
		/// <returns></returns>
		Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData);

		/// <summary>
		///		Gets the Kind of this Document item
		/// </summary>
		[PublicAPI]
		string Kind { get; }

		/// <summary>
		///		The list of Children that are children of this Document item
		/// </summary>
		IList<IDocumentItem> Children { get; }

		/// <summary>
		///		Adds the specified childs.
		/// </summary>
		void Add(params IDocumentItem[] documentChildren);

		/// <summary>
		///		If this is a Natural Document item this defines the Position within the Template where the DocumentItem is parsed from
		/// </summary>
		CharacterLocation ExpressionStart { get; set; }

		/// <summary>
		///		Can be used to allow custom data to be serialized for XML serialization
		/// </summary>
		/// <param name="writer"></param>
		void SerializeXmlCore(XmlWriter writer);

		/// <summary>
		///		Can be used to allow custom data to be deserialized for XML serialization
		/// </summary>
		/// <param name="writer"></param>
		void DeSerializeXmlCore(XmlReader writer);
	}
}