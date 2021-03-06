﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Morestachio.Attributes;
using Morestachio.Document.Custom;
using Morestachio.Formatter;
using Morestachio.Formatter.Framework;
using Morestachio.Framework;

#endregion

namespace Morestachio
{

	/// <summary>
	///		Defines how the Parser should behave when encountering a the PartialStackSize to be exceeded
	/// </summary>
	public enum PartialStackOverflowBehavior
	{
		/// <summary>
		///		Throw a <see cref="MustachioStackOverflowException"/>
		/// </summary>
		FailWithException,
		/// <summary>
		///		Do nothing and skip further calls
		/// </summary>
		FailSilent
	}

	/// <summary>
	///     Options for Parsing run
	/// </summary>
	[PublicAPI]
	public class ParserOptions
	{
		[NotNull]
		private IMorestachioFormatterService _formatters;

		/// <summary>
		///		The store for PreParsed Partials
		/// </summary>
		[CanBeNull]
		public IPartialsStore PartialsStore { get; set; }

		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="template"></param>
		public ParserOptions([NotNull]string template)
			: this(template, null)
		{
		}

		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="template"></param>
		/// <param name="sourceStream">The factory that is used for each template generation</param>
		public ParserOptions([NotNull]string template,
			[CanBeNull]Func<Stream> sourceStream)
			: this(template, sourceStream, null)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		public ParserOptions([NotNull]string template,
			[CanBeNull]Func<Stream> sourceStream,
			[CanBeNull]Encoding encoding)
		{
			Template = template ?? "";
			SourceFactory = sourceStream ?? (() => new MemoryStream());
			Encoding = encoding ?? Encoding.UTF8;
			_formatters = new MorestachioFormatterService();
			Null = string.Empty;
			MaxSize = 0;
			DisableContentEscaping = false;
			WithModelInference = false;
			Timeout = TimeSpan.Zero;
			PartialStackSize = 255;
			CustomDocumentItemProviders = new List<CustomDocumentItemProvider>();
			CultureInfo = CultureInfo.CurrentCulture;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		/// <param name="maxSize">The maximum size.</param>
		/// <param name="disableContentEscaping">if set to <c>true</c> [disable content escaping].</param>
		/// <param name="withModelInference">OBSOLETE</param>
		public ParserOptions([NotNull]string template,
			[CanBeNull]Func<Stream> sourceStream,
			[CanBeNull]Encoding encoding,
			long maxSize,
			bool disableContentEscaping = false)
			: this(template, sourceStream, encoding)
		{
			MaxSize = maxSize;
			DisableContentEscaping = disableContentEscaping;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		/// <param name="disableContentEscaping">if set to <c>true</c> [disable content escaping].</param>
		/// <param name="withModelInference">OBSOLETE</param>
		public ParserOptions([NotNull]string template,
			[CanBeNull]Func<Stream> sourceStream,
			[CanBeNull]Encoding encoding,
			bool disableContentEscaping = false)
			: this(template, sourceStream, encoding, 0, disableContentEscaping)
		{
		}

		/// <summary>
		///		The list of provider that emits custom document items
		/// </summary>
		public IList<CustomDocumentItemProvider> CustomDocumentItemProviders { get; private set; }

		/// <summary>
		///		Enables the Legacy resolver for Formatters names that formatters should contain the name of the formatter as the first argument.
		/// </summary>
		[Obsolete("Enables Legacy behavior for the resolving of formatters. This behavior will be removed completely in later versions", true)]
		public bool LegacyFormatterResolving { get; set; }

		/// <summary>
		///		If set to True morestachio will profile the execution and report the result in both <seealso cref="MorestachioDocumentInfo"/> and <seealso cref=""/>
		/// </summary>
		public bool ProfileExecution { get; set; }

		/// <summary>
		///		Can be used to resolve values from custom objects
		/// </summary>
		public IValueResolver ValueResolver { get; set; }

		/// <summary>
		///		Gets or Sets the Culture in which the template should be rendered
		/// </summary>
		public CultureInfo CultureInfo { get; set; }

		/// <summary>
		///		Can be used to observe unresolved paths
		/// </summary>
		public event InvalidPath UnresolvedPath;

		///// <summary>
		/////		See <see cref="IPartialTemplateProvider"/>
		///// </summary>
		//public IPartialTemplateProvider PartialTemplateProvider { get; set; }

		/// <summary>
		///     Adds an Formatter overwrite or new Formatter for an Type
		/// </summary>
		[NotNull]
		public IMorestachioFormatterService Formatters
		{
			get { return _formatters; }
			set
			{
				_formatters = value ?? throw new InvalidOperationException("You must set the Formatters matcher");
			}
		}

		/// <summary>
		///		Gets or sets the max Stack size for nested Partials in execution. Recommended to be not exceeding 2000. Defaults to 255.
		/// </summary>
		public uint PartialStackSize { get; set; }

		/// <summary>
		///		Defines how the Parser should behave when encountering a the PartialStackSize to be exceeded.
		///		Default is <see cref="PartialStackOverflowBehavior.FailWithException"/>
		/// </summary>
		public PartialStackOverflowBehavior StackOverflowBehavior { get; set; }

		/// <summary>
		///		Gets or sets the timeout. After the timeout is reached and the Template has not finished Processing and Exception is thrown.
		///		For no timeout use <code>TimeSpan.Zero</code>
		/// </summary>
		/// <value>
		/// The timeout.
		/// </value>
		public TimeSpan Timeout { get; set; }

		/// <summary>
		///     The template content to parse.
		/// </summary>
		[NotNull]
		public string Template { get; private set; }

		/// <summary>
		///     In some cases, content should not be escaped (such as when rendering text bodies and subjects in emails).
		///     By default, we use no content escaping, but this parameter allows it to be enabled. Default is False
		/// </summary>
		public bool DisableContentEscaping { get; private set; }

		/// <summary>
		///     Parse the template, and capture paths used in the template to determine a suitable structure for the required
		///     model. Default is False
		/// </summary>
		[Obsolete("This property does nothing and will be removed in future versions")]
		public bool WithModelInference { get; }

		/// <summary>
		///     Defines a Max size for the Generated Template.
		///     Zero for unlimited
		/// </summary>
		public long MaxSize { get; private set; }

		/// <summary>
		///     SourceFactory can be used to create a new stream for each template. Default is
		///     <code>() => new MemoryStream()</code>
		/// </summary>
		[NotNull]
		public Func<Stream> SourceFactory { get; private set; }

		/// <summary>
		///     In what encoding should the text be written
		///     Default is <code>Encoding.Utf8</code>
		/// </summary>
		[NotNull]
		public Encoding Encoding { get; private set; }

		/// <summary>
		///     Defines how NULL values are exposed to the Template default is <code>String.Empty</code>
		/// </summary>
		[NotNull]
		public string Null { get; set; }

		/// <summary>
		///		Allows the creation of an custom Context object
		/// </summary>
		/// <param name="key"></param>
		/// <param name="token"></param>
		/// <param name="value"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public virtual ContextObject CreateContextObject(string key,
			CancellationToken token, 
			object value,
			ContextObject parent = null)
		{
			return new ContextObject(this, key, parent)
			{
				CancellationToken = token,
				Value = value
			};
		}

		internal ParserOptions WithPartial(string partialTemplateTemplate)
		{
			return new ParserOptions(partialTemplateTemplate, SourceFactory, Encoding, DisableContentEscaping)
			{
				Null = Null,
				StackOverflowBehavior = StackOverflowBehavior,
				Formatters = Formatters,
				Timeout = Timeout,
				CultureInfo = CultureInfo
			};
		}

		public ParserOptions CopyWithTemplate(string template)
		{
			return new ParserOptions(template)
			{
				Null = Null,
				StackOverflowBehavior = StackOverflowBehavior,
				Formatters = Formatters,
				Timeout = Timeout,
				CustomDocumentItemProviders = CustomDocumentItemProviders,
				MaxSize = MaxSize,
				DisableContentEscaping = DisableContentEscaping,
				Encoding = Encoding,
				PartialStackSize = PartialStackSize,
				PartialsStore = PartialsStore,
				ProfileExecution = ProfileExecution,
				Template = template,
				ValueResolver = ValueResolver,
				UnresolvedPath = UnresolvedPath,
				SourceFactory = SourceFactory,
				CultureInfo = CultureInfo
			};
		}

		internal void OnUnresolvedPath(string path, Type type)
		{
			UnresolvedPath?.Invoke(path, type);
		}
	}
}