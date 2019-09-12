﻿//-----------------------------------------------------------------------------
// FILE:	    EntityDocument.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Neon.Common;
using Neon.DynamicData;

namespace Couchbase.Lite
{
    /// <summary>
    /// Implements a Couchbase Lite document that provides for accessing document content
    /// via strongly typed properties.
    /// </summary>
    /// <typeparam name="TEntity">The document content type.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="EntityDocument{TEntity}"/> instances are obtained by calling one of 
    /// <see cref="EntityDatabase.CreateEntityDocument{TEntity}()"/>,
    /// <see cref="EntityDatabase.GetEntityDocument{TEntity}(string)"/>,
    /// or <see cref="EntityDatabase.GetExistingEntityDocument{TEntity}(string)"/>.
    /// </para>
    /// <para>
    /// The <see cref="Id"/>, <see cref="Content"/>, <see cref="JObject"/>, and <see cref="Base"/> properties
    /// will be initialized with by these methods.  <see cref="Id"/> will be set to the document ID, <see cref="Content"/> 
    /// returns the document's content as a <typeparamref name="TEntity"/> and <see cref="JObject"/> returns the
    /// content using a JSON.NET <see cref="JObject"/>.  <see cref="Base"/> will be set to the underlying 
    /// Couchbase Lite document.
    /// </para>
    /// <para>
    /// Documents may include additional metadata.  You may set <see cref="Type"/> to a string identifying the
    /// document type (this will be set automatically when you tag your entity interface definition with
    /// a <see cref="DynamicEntityAttribute.Type"/>).  <see cref="Timestamp"/> can be used to indicate the
    /// the document modification time and <see cref="Channels"/> used to communicate channel information
    /// to the Couchbase Sync Gateway.  You may also manage additional properties  directly using the
    /// <see cref="this[string]"/> indexer or <see cref="Properties"/>.  This provide access to all document
    /// properties including internal Couchbase Lite properties.  Here's a list of the reserved metadata 
    /// properties:
    /// </para>
    /// <list type="table">
    /// <item>
    ///     <term><b>_id</b></term>
    ///     <description>
    ///     The Couchbase Lite document ID
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>_rev</b></term>
    ///     <description>
    ///     The Couchbase Lite document revision ID.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>_deleted</b></term>
    ///     <description>
    ///     Indicates that the document has been deleted (Couchbase Lite).
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>_attachments</b></term>
    ///     <description>
    ///     The Couchbase Lite attachment metadata.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>+c</b></term>
    ///     <description>
    ///     Used to persist the document <see cref="Content"/>.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>+t</b></term>
    ///     <description>
    ///     Used to persist the document <see cref="Type"/>.  This is an optional
    ///     string applications may use to uniquely identify the document type.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>+ch</b></term>
    ///     <description>
    ///     Used to persist the document <see cref="Channels"/>.  This is an
    ///     optional string array appplications may use to identify which
    ///     channels where the Couchbase Sync Gateway's sync function assign 
    ///     the document.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><b>+ts</b></term>
    ///     <description>
    ///     Optional used to persist the document <see cref="Timestamp"/>.
    ///     <see cref="Save"/> optionally persists the current <see cref="DateTimeOffset"/>
    ///     here as a standard way for applications to have documents include
    ///     the time at their last modification.
    ///     </description>
    /// </item>
    /// </list>
    /// <note>
    /// Couchbase reserves property names with a leading underscore (<b>_</b>) and 
    /// Neon reservers property names with a leading plus sign (<b>+</b>).
    /// </note>
    /// <note>
    /// Documents created from entities generated by the <b>entity-gen</b> build tool 
    /// and whose defining interfaces were tagged with a non-null or empty <see cref="DynamicEntityAttribute.Type"/>
    /// value will initialze the document <see cref="Type"/> property automatically.
    /// </note>
    /// <para>
    /// Documents will initially be constructed in <b>read-only</b> mode and <see cref="IsReadOnly"/> will
    /// return <c>true</c>.  While read-only, any attempt to modify the document or its contents
    /// will throw an <see cref="InvalidOperationException"/>.  Also, any changes to the underlying
    /// Couchbase Lite document will simply update the document properties, raising all appropriate
    /// change notifications.  No conflict resolutions will be necessary.
    /// </para>
    /// <para>
    /// New documents will be created in <b>read/write</b> mode after having implicity called <see cref="Revise"/>.
    /// For these, you can make your modifications directly and then just call <see cref="Save"/>.
    /// </para>
    /// <para>
    /// To modify an existing document, first call <see cref="Revise"/> to make it <b>read/write</b>.  Then
    /// modify the document properties and <see cref="Content"/> as desired and call <see cref="Save"/>
    /// or <see cref="SaveWithTimestamp"/> to persist the changes.  Use <see cref="Cancel"/> instead 
    /// to undo any modifications and put the document back into the <b>read-only</b> state.
    /// </para>
    /// <para>
    /// The modification status of a document can be monitored by examining the <see cref="IsModified"/>
    /// property, monitoring the <see cref="Changed"/> event, and/or <see cref="INotifyPropertyChanged.PropertyChanged"/>
    /// events at every level of the document content.  These techniques monitor explicit changes to the
    /// content made directly by code or indirectly via data bindings as well as changes to the document
    /// metadata.
    /// </para>
    /// <para>
    /// The <see cref="Delete"/> method marks the document as deleted so the deletion can replicate
    /// to other database.  <see cref="Purge"/> removes the document from the local database but
    /// will not replicate the change.
    /// </para>
    /// <para>
    /// This class implements <see cref="IDynamicEntityContext"/> which enables the linking of entities that
    /// are persisted as the top-level content of a document.  Linking is performed transparently
    /// using the entity's document ID.  Linked entities implement <see cref="INotifyPropertyChanged"/> 
    /// like any other entity, so you may bind your UX to the properties.
    /// </para>
    /// <para><b><u>Attachments</u></b></para>
    /// <para>
    /// Entity documents include support for Couchbase Mobile attachments.  The <see cref="Attachments"/>  
    /// and <see cref="AttachmentNames"/> properties enumerate the attachments and their names.
    /// <see cref="SetAttachment(string, byte[], string)"/> and <see cref="SetAttachment(string, Stream, string)"/> 
    /// add a new attachment or modify an existing one, <see cref="GetAttachment(string)"/> returns a
    /// named attachment (or <c>null</c> if it doesn't exist, and <see cref="RemoveAttachment(string)"/> 
    /// can be used to delete an attachment.
    /// </para>
    /// <note>
    /// Be careful to call <see cref="Attachment.Dispose()"/> when you're done with an attachment.
    /// This ensures that the underlying file stream is closed.
    /// </note>
    /// <note>
    /// You can enumerate or get attachments for read-only and read/write documents.  The set and remove
    /// methods may only be called for read/write documents.
    /// </note>
    /// <para>
    /// The <see cref="AttachmentEvent"/> is designed to make it easy for derived classes to implement
    /// the <see cref="INotifyPropertyChanged"/> pattern for attachments as well as to provide for
    /// easily data binding image attachments to a user interface.  These derived classes are called
    /// <b>binder documents</b>.
    /// </para>
    /// <para><b><u>Binder Documents</u></b></para>
    /// <para>
    /// Binder documents are specialized Couchbase Lite document classes that derive from 
    /// <see cref="EntityDocument{TEntity}"/> and are generated using the same <b>entity-gen</b>
    /// tool used to generate the entity classes.  Generated binder documents classes include
    /// zero or more properties that map to Couchbase Lite attachments.
    /// </para>
    /// <para>
    /// You'll need to create an <c>interface</c> describing your document within a model
    /// assembly.  This interface needs to be tagged with a <see cref="BinderDocumentAttribute"/> 
    /// specifying the document entity type and optionally customizing the generated document
    /// class namespace and name.
    /// </para>
    /// <para>
    /// Your binder document interface can define one or more properties to be associated 
    /// with a document attachment.  This property must be tagged with a <see cref="BinderAttachmentAttribute"/>,
    /// be a <c>string</c>, and have a getter but not a setter.  This attribute includes an optional property
    /// that allows for customizing the persisted attachment name.
    /// </para>
    /// <para>
    /// The <b>entity-gen</b> tool will generate binder document classes with public read-only properties
    /// for each defined attachment.  These properties return the file system path to the attachment or
    /// <c>null</c> if the attachment does not exist.  <b>entity-gen</b> also generates code that monitors
    /// for attachment changes.  When a change is detected, the associated property will be updated and
    /// an <see cref="INotifyPropertyChanged"/> notification will be raised.
    /// </para>
    /// <note>
    /// The currently supported scenario allowing a user interface to bind an image control to an
    /// document image attachment.  The binder's attachment properties return the attachment path as
    /// a <c>string</c> and the UX platform converts this to an <b>ImageSource</b> using the
    /// <b>ImageSourceConverter</b> or potentially a custom converter.
    /// </note>
    /// <para>
    /// The <see cref="EntityDatabase"/> class provides the <see cref="EntityDatabase.CreateBinderDocument{TDocument}"/>,
    /// <see cref="EntityDatabase.GetBinderDocument{TDocument}(string)"/>, and <see cref="EntityDatabase.GetExistingBinderDocument{TDocument}(string)"/> 
    /// methods for creating or retrieving binder documents from a database.
    /// </para>
    /// <para>
    /// Once you have a binder document instance, the <see cref="EntityDocument{TEntity}.SetAttachment(string, byte[], string)"/>,
    /// <see cref="EntityDocument{TEntity}.SetAttachment(string, Stream, string)"/>, and <see cref="EntityDocument{TEntity}.GetAttachment(string)"/>
    /// methods can be used to manipulate the document's attachments.  These methods will work for attachments with names that
    /// map to binder document interface properties as well as any other property.  For ease of use, the <c>entity-gen</c> tool
    /// also generates specific methods to set, get, and delete each defined attachment property.
    /// </para>
    /// </remarks>
    /// <threadsafety instance="false"/>
    public class EntityDocument<TEntity> : NotifyPropertyChanged, IEntityDocument
        where TEntity : class, IDynamicEntity, new()
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Holds information about a document attachment.
        /// </summary>
        private class AttachmentInfo
        {
            /// <summary>
            /// The attachment name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// <para>
            /// Returns the path to the attachment contents in the local file system.
            /// </para>
            /// <note>
            /// For persisted attachments, this will reference the attachment in the
            /// Couchbase Lite database.  For unsaved attachments, this will reference
            /// a temporary file and for deleted attachments, this will return <c>null</c>.
            /// </note>
            /// </summary>
            public string Path { get; set; }
        }

        //---------------------------------------------------------------------
        // Static members

        private const string UnattachedError      = "This document is not attached to a database.";
        private const string ReadOnlyError        = "This document is read only.";
        private const string AlreadyLoadedError   = "This document is already fully materialized.";
        private const string ReviseQueryRowError  = "Unable to revise a document returned by a query directly.  Use {EntityDocument._Load()] to materialize the document";
        private const string ReviseReadWriteError = "This document is already in the read/write state.";
        private const string SaveReadOnlyError    = "Cannot save a read-only document.";
        private const string CancelReadOnlyError  = "This document is already in the read-only state.";

        /// <summary>
        /// Constructs a binder document of a specific generic type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="document">The document.</param>
        /// <returns>The binder document.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if <typeparamref name="TDocument"/> was not previously registered by a call to 
        /// <see cref="EntityDatabase.Register(Func{Document, IEntityDocument}, Func{IDictionary{string, object}, EntityDatabase, Revision, IEntityDocument}, string[])"/>.
        /// </exception>
        internal static TDocument Create<TDocument>(Document document)
            where TDocument : class, IEntityDocument
        {
            return (TDocument)Create(typeof(TDocument), document);
        }

        /// <summary>
        /// Constructs a binder document of a specific type.
        /// </summary>
        /// <param name="documentType">The document type.</param>
        /// <param name="document">The document.</param>
        /// <returns>The binder document.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if <paramref name="documentType"/> was not previously registered by a call to 
        /// <see cref="EntityDatabase.Register(Func{Document, IEntityDocument}, Func{IDictionary{string, object}, EntityDatabase, Revision, IEntityDocument}, string[])"/>.
        /// </exception>
        internal static IDynamicDocument Create(Type documentType, Document document)
        {
            Covenant.Requires<ArgumentNullException>(documentType != null);
            Covenant.Requires(typeof(IEntityDocument).IsAssignableFrom(documentType), $"Type [{documentType.FullName}] does not implement [{nameof(IEntityDocument)}].");

            var derivedCreateInfo = EntityDatabase.GetDocumentCreateInfo(documentType);
            var derivedDocument = derivedCreateInfo.AttachedCreator(document);

            derivedDocument.SetAttachmentTracking(derivedCreateInfo.AttachmentNames);

            return (IDynamicDocument)derivedDocument;
        }

        //---------------------------------------------------------------------
        // Instance members

        private Revision                            revision;               // The document revision currently mapped to the content.
        private JObject                             jObject;                // The content JObject
        private bool                                wasDeleted;
        private Dictionary<string, bool>            deletedAttachments;     // Workaround for https://github.com/couchbase/couchbase-lite-net/issues/661
        private HashSet<string>                     attachmentTracking;     // Set of case insenstive binder attachment names we'll be tracking
                                                                            // or NULL for non-binder documents.
        private Dictionary<string, AttachmentInfo>  binderAttachments;      // Maps case insenstive attachment names to attachment information for
                                                                            // binder documents or NULL for non-binder documents.

        // This counter is used to disable read-only tests by the [Changed] event handler
        // while the document is being updated with content read from the database.  The
        // value will be zero when read-only tests are to be performed and postive when
        // they are not.
        //
        // Use the [ReadOnlyCheck()] method to test whether testing is to be performed
        // and [PushReadOnlyCheck()] and [PopReadOnlyCheck()] to increment and decrement
        // the counter.

        private int readOnlyCheck = 0;

        private bool ReadOnlyCheck()
        {
            return readOnlyCheck == 0;
        }

        private void PushReadOnlyCheck()
        {
            readOnlyCheck++;
        }

        private void PopReadOnlyCheck()
        {
            readOnlyCheck--;

            if (readOnlyCheck < 0)
            {
                throw new InvalidOperationException("Internal: ReadOnlyCheck underflow.");
            }
        }

        /// <summary>
        /// Called by derived classes to initialize a specialized <b>read-only</b> document 
        /// that wraps document properties but not the document itself.  This is used to 
        /// generate the documents passed to entity view map functions or constructed from 
        /// <see cref="QueryRow"/> instances.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="database">The parent <see cref="EntityDatabase"/>.</param>
        /// <param name="revision">The revision.</param>
        protected EntityDocument(IDictionary<string, object> properties, EntityDatabase database, Revision revision)
            : this(Stub.Param, properties, database, revision)
        {
        }

        /// <summary>
        /// Constructs a specialized <b>read-only</b> document that wraps document properties
        /// but not the document itself.  This is used to generate the documents passed to
        /// entity view map functions or constructed from <see cref="QueryRow"/> instances.
        /// </summary>
        /// <param name="stub">Stub parameter used to disambiguage constructors.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="database">The parent <see cref="EntityDatabase"/>.</param>
        /// <param name="revision">The optional revision.</param>
        /// <remarks>
        /// <note>
        /// The <see cref="Base"/> property will return <c>null</c> for documents
        /// created with this constructor.
        /// </note>
        /// </remarks>
        internal EntityDocument(Stub.Value stub, IDictionary<string, object> properties, EntityDatabase database, Revision revision = null)
        {
            Covenant.Requires<ArgumentNullException>(properties != null);
            Covenant.Requires<ArgumentNullException>(database != null);

            this.Id         = (string)properties["_id"];
            this.Database   = database;
            this.revision   = revision;
            this.Base       = null;
            this.Properties = properties;
            this.IsModified = false;
            this.IsReadOnly = true;

            // Load the deletion status of the document (if present).

            object rawDeleted;

            if (properties.TryGetValue("_deleted", out rawDeleted))
            {
                wasDeleted = (bool)rawDeleted;
            }

            // Load the content from the properties.

            jObject = GetContentJObject(properties);
            Content = DynamicEntity.Create<TEntity>(jObject, database);

            // Initialize the entity link as the document ID to provide for 
            // referencing this entity from another by linking.

            Content._SetLink(this.Id);

            // Monitor for metadata and content entity changes.

            Content.Changed += OnChanged;
        }

        /// <summary>
        /// Called by derived classes to initialize an instance wrapping a Couchbase Lite 
        /// <see cref="Document"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        protected EntityDocument(Document document)
            : this(Stub.Param, document)
        {
        }

        /// <summary>
        /// Constructs an instance wrapping a Couchbase Lite <see cref="Document"/>.
        /// </summary>
        /// <param name="stub">Stub parameter used to disambiguage constructors.</param>
        /// <param name="document">The document.</param>
        /// <remarks>
        /// <note>
        /// New documents will be initialized in <b>read/write</b> mode where as existing
        /// document are initialized as <b>read-only</b>.
        /// </note>
        /// </remarks>
        internal EntityDocument(Stub.Value stub, Document document)
        {
            Covenant.Requires<ArgumentNullException>(document != null);

            var isNewDocument = document.CurrentRevisionId == null;

            this.Id         = document.Id;
            this.Database   = EntityDatabase.From(document.Database);
            this.Base       = document;
            this.IsModified = isNewDocument;
            this.IsReadOnly = !isNewDocument;

            // Configure the event listeners.

            WeakEventController.AddHandler<Document, Document.DocumentChangeEventArgs>(Base, nameof(Base.Change), OnDocumentChanged);

            // Obtain the document revision we're going to use to map to the content
            // and setup the document change event handlers.  For R/W documents, we're
            // going to create a new revision where any content changes will go.
            
            if (IsReadOnly)
            {
                revision = document.CurrentRevision;
            }
            else
            {
                revision = document.CreateRevision();
            }

            Properties = revision.Properties;

            // Load the content from the document revision.

            jObject = GetContentJObject(Properties);

            if (!Properties.ContainsKey(NeonPropertyNames.Content))
            {
                Properties.Add(NeonPropertyNames.Content, jObject);
            }

            Content = DynamicEntity.Create<TEntity>(jObject, Database);

            // Initialize the entity link as the document ID to provide for 
            // referencing this entity from another by linking.

            Content._SetLink(this.Id);

            // Set the document type if the document is new and the entity defined
            // a type.

            if (isNewDocument)
            {
                var entityType = Content._GetEntityType();

                if (!string.IsNullOrEmpty(entityType))
                {
                    this.Type = entityType;
                }
            }

            // Monitor for metadata and content entity changes.

            Content.Changed += OnChanged;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~EntityDocument()
        {
            if (Base == null)
            {
                return;
            }

            WeakEventController.RemoveHandler<Document, Document.DocumentChangeEventArgs>(Base, nameof(Base.Change), OnDocumentChanged);
        }

        /// <summary>
        /// Returns the underlying Couchbase document or <c>null</c> for documents returned
        /// as entity query rows.
        /// </summary>
        public Document Base { get; private set; }

        /// <summary>
        /// Returns the entity database associated with this document.
        /// </summary>
        public EntityDatabase Database { get; private set; }

        /// <inheritdoc/>
        public Revision Revision
        {
            get { return revision; }
        }

        /// <summary>
        /// Returns <c>true</c> if the document is currently read only.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if the document contents or metadata has been modified since 
        /// it was last read or written to the database.
        /// </summary>
        /// <remarks>
        /// Newly created documents are implicitly considered to be modified.
        /// </remarks>
        public bool IsModified { get; private set; }

        /// <summary>
        /// Raised when the document contents or metadata have been modified.
        /// </summary>
        public event EventHandler<EventArgs> Changed;

        /// <inheritdoc/>
        void IEntityDocument.SetAttachmentTracking(HashSet<string> attachmentNames)
        {
            if (attachmentNames != null)
            {
                attachmentTracking = attachmentNames;
                binderAttachments  = new Dictionary<string, AttachmentInfo>();

                // Raise the [AttachmentEvent] for each attachment with a non-null
                // file path so the binder class can intialize its attachment properties.
                // Note that we're disabling property change notification for this
                // initialization.

                foreach (var attachmentName in attachmentNames)
                {
                    var attachmentPath = GetAttachmentPath(attachmentName);

                    if (attachmentPath == null)
                    {
                        continue;
                    }

                    AttachmentEvent?.Invoke(this,
                        new AttachmentEventArgs()
                        {
                            Name   = attachmentName,
                            Path   = attachmentPath,
                            Notify = false
                        });
                }
            }
        }

        /// <summary>
        /// Raised during important events in the document's attachment lifecycle so that
        /// derived documents can implement <see cref="INotifyPropertyChanged"/> for
        /// attachments.  See <see cref="EntityDocument{TEntity}"/> remarks for details 
        /// on how to use this.
        /// </summary>
        protected event EventHandler<AttachmentEventArgs> AttachmentEvent;

        /// <summary>
        /// Returns <c>true</c> if the document has been deleted or purged.
        /// </summary>
        public bool IsDeleted
        {
            get
            {
                Covenant.Requires<InvalidOperationException>(Base != null, UnattachedError);

                return wasDeleted || (revision != null && (revision.IsDeletion || revision.IsGone));
            }
        }

        /// <summary>
        /// The document ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Returns the document content as a <typeparamref name="TEntity"/>.
        /// </summary>
        public TEntity Content { get; private set; }

        /// <summary>
        /// Optionally specifies the document type.
        /// </summary>
        /// <remarks>
        /// <note>
        /// Setting this to <c>null</c> will remove the property.
        /// </note>
        /// </remarks>
        public string Type
        {
            get { return this[NeonPropertyNames.Type] as string; }
            set { this[NeonPropertyNames.Type] = value; }
        }

        /// <summary>
        /// Optionally indicates the last time (UTC) the document was modified.
        /// </summary>
        /// <remarks>
        /// <note>
        /// The <see cref="Timestamp"/> property value should be taken with a grain of salt
        /// since it will often be set on mobile devices using their clocks and assuming that
        /// they are also aware of the correct timezone.
        /// </note>
        /// </remarks>
        public DateTime? Timestamp
        {
            get { return (DateTime?)this[NeonPropertyNames.Timestamp]; }
            set { this[NeonPropertyNames.Timestamp] = value; }
        }

        /// <inheritdoc/>
        public Type EntityType
        {
            get { return typeof(TEntity); }
        }

        /// <inheritdoc/>
        public string _GetLink()
        {
            return Id;
        }
        
        /// <summary>
        /// Optionally specifies the document channels.
        /// </summary>
        /// <remarks>
        /// <note>
        /// Setting this to <c>null</c> or an empty array will remove the property.
        /// </note>
        /// <note>
        /// Avoid indexing into this property due to possible poor performance.  Enumerate it or 
        /// make a copy of the array instead.
        /// </note>
        /// </remarks>
        public string[] Channels
        {
            get
            {
                // $todo(jeff.lill):
                //
                // Performance could be improved by loading and caching this.  It's probably
                // going to be rare for client apps to examine the channels though (they'll
                // mostly be setting this) so I'm going to defer this will later.

                var channels = this[NeonPropertyNames.Channels] as string[];

                if (channels != null)
                {
                    return channels;
                }

                var jArray = this[NeonPropertyNames.Channels] as JArray;

                if (jArray == null || jArray.Count == 0)
                {
                    return null;
                }

                channels = new string[jArray.Count];

                var i = 0;

                foreach (var item in jArray)
                {
                    if (item.Type == JTokenType.String)
                    {
                        channels[i] = item.ToString();
                    }

                    i++;
                }

                return channels;
            }

            set
            {
                if (value != null && value.Length > 0)
                {
                    this[NeonPropertyNames.Channels] = value;
                }
                else
                {
                    Properties.Remove(NeonPropertyNames.Channels);
                }

                OnChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Indexes into the document's top-level properties.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <returns>The property value or <c>null</c> if the property does not exist.</returns>
        /// <remarks>
        /// <para>
        /// The top-level properties hold the document metadata as well as a reference to its
        /// content.  Properties such as <see cref="Type"/> and <see cref="Channels"/> properties
        /// are simply stanardized shortcuts.  You may use this to manage additional metadata.
        /// </para>
        /// <note>
        /// There's no way to distunguish between a property that exists that's set to <c>null</c>
        /// and one that doesn't exist.  Use <see cref="Properties"/> for finer control.
        /// </note>
        /// </remarks>
        public object this[string key]
        {
            get
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

                object value;

                if (Properties.TryGetValue(key, out value))
                {
                    return value;
                }

                return null;
            }

            set
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(key));

                // Map the property key to one of the built-in document properties.

                string propertyName = null;

                switch (key)
                {
                    case NeonPropertyNames.Type:

                        propertyName = nameof (this.Type);
                        break;

                    case NeonPropertyNames.Content:

                        propertyName = nameof(this.Content);
                        break;

                    case NeonPropertyNames.Timestamp:

                        propertyName = nameof(this.Timestamp);
                        break;
                }

                // Update the value and raise a changed event for the associated
                // built-in document property (if any).

                if (value == null)
                {
                    if (Properties.ContainsKey(key))
                    {
                        Properties.Remove(key);

                        if (propertyName != null)
                        {
                            OnPropertyChanged(propertyName);
                        }
                    }
                }
                else
                {
                    if (!Properties.ContainsKey(key) || !object.Equals(Properties[key], value))
                    {
                        Properties[key] = value;

                        if (propertyName != null)
                        {
                            OnPropertyChanged(propertyName);
                        }
                    }
                }

                // $todo(jeff.lill): 
                //
                // For now, we're going to consider any metadata assignment to be a change
                // without actually comparing values.

                OnChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Returns a dictionary including all of the document's top-level properties.  This includes
        /// both internal Couchbase Lite properties, extended properties such as <see cref="Content"/>,
        /// <see cref="Type"/> and <see cref="Channels"/> as well as application defined properties.
        /// </summary>
        public IDictionary<string, object> Properties { get; private set; }

        /// <summary>
        /// Returns the existing content <see cref="JObject"/> from a property 
        /// dictionary or creates a new instance.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <returns>The content <see cref="JObject"/>.</returns>
        private JObject GetContentJObject(IDictionary<string, object> properties)
        {
            object rawContent;

            if (properties.TryGetValue(NeonPropertyNames.Content, out rawContent))
            {
                return (JObject)rawContent;
            }
            else
            {
                return new JObject();
            }
        }

        /// <summary>
        /// Materializes a fully functional document from a document returned as a query row.
        /// </summary>
        /// <returns>The materialized document.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document is already fully materialized.</exception>
        public EntityDocument<TEntity> Materialize()
        {
            Covenant.Requires<InvalidOperationException>(Base == null, AlreadyLoadedError);

            if (Base != null)
            {
                throw new InvalidOperationException(AlreadyLoadedError);
            }

            return Database.GetExistingEntityDocument<TEntity>(Id);
        }

        /// <summary>
        /// Puts the document into <b>read/write</b> mode so it can be modified.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the document was constructed from a query row.</exception>
        /// <remarks>
        /// <para>
        /// This method may be called safely when the document is already in the read/write state.
        /// </para>
        /// <note>
        /// It is important that all documents that are opened for revision are either
        /// persisted via <see cref="Save"/> or reverted via <see cref="Cancel"/>.  Doing
        /// this ensures that resources associated with the revision are released.  This
        /// is particularily important for derived documents that track attachments since
        /// unsaved attachments are persisted to temporary storage.
        /// </note>
        /// </remarks>
        public void Revise()
        {
            Covenant.Requires<InvalidOperationException>(Base != null, ReviseQueryRowError);

            if (Base == null)
            {
                throw new InvalidOperationException(ReviseQueryRowError);
            }

            if (!IsReadOnly)
            {
                return; // We're already revising.
            }

            revision   = ((SavedRevision)revision).CreateRevision();
            Properties = revision.Properties;

            var newJObject = (JObject)GetContentJObject(Properties).DeepClone();

            try
            {
                PushReadOnlyCheck();
                Content._Load(newJObject, reload: true);
            }
            finally
            {
                PopReadOnlyCheck();
            }

            Properties[NeonPropertyNames.Content] = newJObject;
            jObject                               = newJObject;
            IsReadOnly                            = false;

            if (binderAttachments != null)
            {
                binderAttachments.Clear();
            }
        }

        /// <summary>
        /// Persists any document changes to the database and then reverts the document
        /// back into <b>read-only</b> mode.
        /// </summary>
        /// <param name="policy">The optional conflict resolution policy that overrides <see cref="ConflictPolicy.Default"/>.</param>
        /// <returns>The <see cref="SavedRevision"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to save a <b>read-only</b> document.</exception>
        /// <exception cref="ConflictException">Thrown a document conflict could not be resolved by the conflict policy.</exception>
        /// <remarks>
        /// <para>
        /// By default, method will quietly persist conflicting revisions to the database.
        /// You can change this behavior by passing a built-in or custom conflict policy
        /// or setting <see cref="ConflictPolicy.Default"/> to a different policy.
        /// </para>
        /// <para>
        /// See <see cref="ConflictPolicy"/> for more information.
        /// </para>
        /// </remarks>
        public SavedRevision Save(ConflictPolicy policy = null)
        {
            Covenant.Requires<InvalidOperationException>(!IsReadOnly, SaveReadOnlyError);

            if (IsReadOnly)
            {
                throw new InvalidOperationException(SaveReadOnlyError);
            }

            policy = policy ?? ConflictPolicy.Default;

            var unsavedRevision = revision as UnsavedRevision;

            Covenant.Assert(unsavedRevision != null);

            IsReadOnly = true;

            try
            {
                PushReadOnlyCheck();

                revision = unsavedRevision.Save(policy.Type == ConflictPolicyType.Ignore);

                unsavedRevision.Dispose();
                HandleSavedAttachments();

                return (SavedRevision)revision;
            }
            catch (CouchbaseLiteException e1)
            {
                if (e1.CBLStatus.Code != StatusCode.Conflict)
                {
                    throw; // Rethrow all but conflict errors
                }

                if (policy.Type == ConflictPolicyType.Fail)
                {
                    throw new ConflictException();
                }

                var details = new ConflictDetails()
                {
                    Document             = Base,
                    EntityDocument       = this,
                    UnsavedRevision      = unsavedRevision,
                    ConflictingRevisions = Base.ConflictingRevisions.OrderByDescending(r => r.Id).ToArray()
                };

                try
                {
                    policy.Resolve(details);

                    if (details.SavedRevision == null)
                    {
                        throw new ConflictException($"[{policy.GetType().Name}]: Failed to resolve a [{details.EntityDocument.EntityType.FullName}] conflict.");
                    }

                    revision = details.SavedRevision;

                    unsavedRevision.Dispose();
                    HandleSavedAttachments();

                    return (SavedRevision)revision;
                }
                catch (CouchbaseLiteException e2)
                {
                    if (e2.CBLStatus.Code != StatusCode.Conflict)
                    {
                        throw;
                    }
                    else
                    {
                        throw new ConflictException($"[{policy.GetType().Name}]: Failed to resolve a [{details.EntityDocument.EntityType.FullName}] conflict.");
                    }
                }
            }
            finally
            {
                IsModified = false;
                deletedAttachments = null;

                PopReadOnlyCheck();
            }

            // NOTE: 
            //
            // We don't need to reload the document here.  We'll do that when
            // we handle the document changed event.
        }

        /// <summary>
        /// Performs document save related activities for attachments including deleting
        /// any temporary files and raising the required <see cref="INotifyPropertyChanged"/> 
        /// notifications.
        /// </summary>
        private void HandleSavedAttachments()
        {
            if (binderAttachments == null)
            {
                return;
            }

            foreach (var attachmentInfo in binderAttachments.Values)
            {
                if (attachmentInfo.Path == null)
                {
                    // Attachment was deleted.

                    AttachmentEvent?.Invoke(this,
                        new AttachmentEventArgs()
                        {
                            Name = attachmentInfo.Name,
                            Path = null
                        });
                }
                else
                {
                    // Attachment was set.

                    AttachmentEvent?.Invoke(this,
                        new AttachmentEventArgs()
                        {
                            Name = attachmentInfo.Name,
                            Path = GetAttachmentPath(attachmentInfo.Name)
                        });

                    File.Delete(attachmentInfo.Path);
                }
            }

            binderAttachments.Clear();
        }

        /// <summary>
        /// Persists any document changes to the database, setting <see cref="Timestamp"/> to
        /// the current time (UTC) and then reverts the document back into <b>read-only</b> mode.
        /// </summary>
        /// <param name="policy">The optional conflict resolution policy that overrides <see cref="ConflictPolicy.Default"/>.</param>
        /// <returns>The <see cref="SavedRevision"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to save a <b>read-only</b> document.</exception>
        /// <exception cref="ConflictException">Thrown a document conflict could not be resolved by the conflict policy.</exception>
        /// <remarks>
        /// <para>
        /// By default, method will quietly persist conflicting revisions to the database.
        /// You can change this behavior by passing a built-in or custom conflict policy
        /// or setting <see cref="ConflictPolicy.Default"/> to a different policy.
        /// </para>
        /// <para>
        /// See <see cref="ConflictPolicy"/> for more information.
        /// </para>
        /// </remarks>
        public SavedRevision SaveWithTimestamp(ConflictPolicy policy = null)
        {
            Covenant.Requires<InvalidOperationException>(!IsReadOnly, SaveReadOnlyError);

            if (IsReadOnly)
            {
                throw new InvalidOperationException(SaveReadOnlyError);
            }

            if (IsModified)
            {
                Timestamp = DateTime.UtcNow;
            }

            return Save(policy);
        }

        /// <summary>
        /// Reverts any document modifications and puts the document back into
        /// <b>read-only</b> mode.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the document is already <b>read-only</b>.</exception>
        public void Cancel()
        {
            Covenant.Requires<InvalidOperationException>(IsReadOnly, CancelReadOnlyError);

            if (IsReadOnly)
            {
                throw new InvalidOperationException(CancelReadOnlyError);
            }

            ((UnsavedRevision)revision).Dispose();

            if (Base.CurrentRevision == null)
            {
                return; // This is a NOP for a new document that has no revisions.
            }

            // Reload the current revision content into the model.

            revision   = Base.CurrentRevision;
            Properties = revision.Properties;
            jObject    = GetContentJObject(Properties);

            Content._Load(jObject, reload: true);

            IsModified         = false;
            IsReadOnly         = false;
            deletedAttachments = null;

            // For binder documents, we need to raise the [AttachmentEvent] for each
            // tracked attachment that was modified, passing the original attachment's
            // persisted path, or NULL if the attachment didn't originally exist.

            if (binderAttachments?.Count > 0)
            {
                foreach (var attachmentInfo in binderAttachments.Values)
                {
                    if (attachmentInfo == null)
                    {
                        // The attachment didn't originally exist.

                        AttachmentEvent?.Invoke(this,
                            new AttachmentEventArgs()
                            {
                                Name = attachmentInfo.Name,
                                Path = null
                            });
                    }
                    else
                    {
                        // The attachment modification was undone.

                        AttachmentEvent?.Invoke(this,
                            new AttachmentEventArgs()
                            {
                                Name = attachmentInfo.Name,
                                Path = GetAttachmentPath(attachmentInfo.Name)
                            });
                    }
                }

                binderAttachments.Clear();
            }
        }

        /// <summary>
        /// Removes the document from the database, cancelling any pending changes.
        /// </summary>
        public void Delete()
        {
            Covenant.Requires<InvalidOperationException>(Base != null, UnattachedError);

            if (!IsReadOnly)
            {
                Cancel();
            }

            Base.Delete();

            wasDeleted = true;
        }

        /// <summary>
        /// Completely purges the document from the local database without replicating the
        /// change to other databases.  This differs from the <see cref="Delete"/> method
        /// that marks the document as deleted so the operation can be replicated to
        /// other database instances.
        /// </summary>
        public void Purge()
        {
            Covenant.Requires<InvalidOperationException>(Base != null, UnattachedError);
            Covenant.Requires<InvalidOperationException>(!IsReadOnly, ReadOnlyError);

            if (IsReadOnly)
            {
                throw new InvalidOperationException(ReadOnlyError);
            }

            Base.Purge();
        }

        /// <summary>
        /// Called when the underlying Couchbase Lite document has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        private void OnDocumentChanged(object sender, Document.DocumentChangeEventArgs args)
        {
            // $todo(jeff.lill):
            //
            // I'm currently ignoring any conflicts.  Eventually we'll want to
            // expose an event (or something) that provides a way for the application
            // to react to conflicting changes.

            if (!args.Change.IsCurrentRevision)
            {
                return;
            }

            if (!IsReadOnly)
            {
                // We're going to ignore document changes while the document
                // is being revised.  This means that we'll probably end up 
                // having to resolve a conflict later when saving.
                //
                // In theory we could give the application a chance to react
                // here, but I'd rather keep things simple and handle conflict
                // resolution in one place (during save).

                return;
            }

            try
            {
                PushReadOnlyCheck();

                if (Base.Deleted)
                {
                    wasDeleted = true;
                    revision   = null;

                    Properties.Clear();
                    OnChanged(this, EventArgs.Empty);
                    return;
                }

                // Reload the document properties.

                var changed          = false;
                var currentType      = this.Type;
                var currentChannels  = this.Channels;
                var currentTimestamp = this.Timestamp;

                revision   = Base.CurrentRevision;
                Properties = revision.Properties;

                // Raise property changed events for the built-in properties.

                if (!object.Equals(currentType, this.Type))
                {
                    OnPropertyChanged(nameof(this.Type));
                    changed = true;
                }

                if (!NeonHelper.SequenceEqual(currentChannels, this.Channels))
                {
                    OnPropertyChanged(nameof(this.Channels));
                    changed = true;
                }

                if (!object.Equals(currentTimestamp, this.Timestamp))
                {
                    OnPropertyChanged(nameof(this.Timestamp));
                    changed = true;
                }

                // Load the content from the document revision (this
                // will implicitly raise property changed events).

                jObject = GetContentJObject(Properties);

                if (!Properties.ContainsKey(NeonPropertyNames.Content))
                {
                    Properties.Add(NeonPropertyNames.Content, jObject);
                }

                changed = Content._Load(jObject, reload: true) || changed;

                // Raise any necessary [AttachmentEvents].

                if (attachmentTracking != null)
                {
                    foreach (var name in attachmentTracking)
                    {
                        AttachmentEvent?.Invoke(this,
                            new AttachmentEventArgs()
                            {
                                Name = name,
                                Path = GetAttachmentPath(name)
                            });
                    }
                }

                // Raise the document changed event if the document
                // loaded had differences from the existing one.

                if (changed)
                {
                    OnChanged(this, EventArgs.Empty);
                }
            }
            finally
            {
                PopReadOnlyCheck();
            }
        }

        /// <summary>
        /// Called to when the document has changed to raise the <see cref="Changed"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        private void OnChanged(object sender, EventArgs args)
        {
            if (IsReadOnly && ReadOnlyCheck())
            {
                throw new InvalidOperationException(ReadOnlyError);
            }

            IsModified = !IsReadOnly;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Enumerates the document's attachment names.
        /// </summary>
        public IEnumerable<string> AttachmentNames
        {
            get
            {
                if (deletedAttachments == null)
                {
                    return revision.AttachmentNames;
                }

                // $todo(jeff.lill):
                //
                // Couchbase Lite appears to have a bug where it sometimes returns a
                // non-NULL attachment after its been deleted.  I've reported this
                // as issue #661:
                //
                //      https://github.com/couchbase/couchbase-lite-net/issues/661

                return revision.AttachmentNames.Where(n => !deletedAttachments.ContainsKey(n));
            }
        }

        /// <summary>
        /// Enumerates the document's attachments.
        /// </summary>
        public IEnumerable<Attachment> Attachments
        {
            get
            {
                if (deletedAttachments == null)
                {
                    return revision.Attachments;
                }

                // $todo(jeff.lill):
                //
                // Couchbase Lite appears to have a bug where it sometimes returns a
                // non-NULL attachment after its been deleted.  I've reported this
                // as issue #661:
                //
                //      https://github.com/couchbase/couchbase-lite-net/issues/661

                return revision.Attachments.Where(a => !deletedAttachments.ContainsKey(a.Name));
            }
        }

        /// <summary>
        /// Returns the file system path to the persisted named attachment.
        /// </summary>
        /// <param name="name">The attachment name.</param>
        /// <returns>The file system path or <c>null</c> if the attachment doesn't exist.</returns>
        private string GetAttachmentPath(string name)
        {
#if DISABLED
            // $note(jeff.lill): 
            //
            // This is the safe, but slow way.  This requires that
            // we actually open the attachment file.

            var attachment = revision.GetAttachment(name);

            if (attachment == null)
            {
                return null;
            }

            using (attachment)
            {
                return ((FileStream)attachment.ContentStream).Name;
            }
#else
            // $hack(jeff.lill):
            //
            // This is the fragile but fast way to get the persisted attachment path.
            // We're going to access the internal attachment metadata, extract the
            // content digest and convert that into the file path.
            //
            // This depends on how Couchbase Lite stores attachments in the filesystem
            // and formats the attachment metadata properties.

            // Read/write documents encode attachment metadata as a [Dictionary<string, object>]
            // or a [JObject].

            object rawAttachments;

            if (!revision.Properties.TryGetValue("_attachments", out rawAttachments))
            {
                return null; // Document has no attachments.
            }

            string digest;

            var dictionaryAttachments = rawAttachments as Dictionary<string, object>;

            if (dictionaryAttachments != null)
            {
                object rawMetadata;

                if (!dictionaryAttachments.TryGetValue(name, out rawMetadata))
                {
                    return null; // Attachment doesn't exist.
                }

                var metadata = rawMetadata as IDictionary<string, object>;

                if (metadata == null)
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                object rawDigest;

                if (!metadata.TryGetValue("digest", out rawDigest))
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                digest = rawDigest as string;

                if (digest == null)
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }
            }
            else
            {
                var jObjectAttachments = rawAttachments as JObject;

                if (jObjectAttachments == null)
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                JToken attachmentToken;

                if (!jObjectAttachments.TryGetValue(name, out attachmentToken))
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                var attachmentJObject = attachmentToken as JObject;

                if (attachmentToken == null)
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                JToken digestToken;

                if (!attachmentJObject.TryGetValue("digest", out digestToken))
                {
                    throw new NotImplementedException(EntityDatabase.NotCompatibleError);
                }

                digest = (string)digestToken;
            }

            // Assuming that we can form the attachment file path by extracting
            // everything after the first dash (-) in the digest, Base64 decoding
            // the digest and the converting it to upper case HEX, and finally,
            // appending the  ".blob" file type and then prepending the database's 
            // attachments folder path.

            var dashPos = digest.IndexOf('-');

            if (dashPos == -1)
            {
                throw new NotImplementedException(EntityDatabase.NotCompatibleError);
            }

            var digestBase64 = digest.Substring(dashPos + 1);
            var digestBytes  = Convert.FromBase64String(digestBase64);
            var digestHex    = NeonHelper.ToHex(digestBytes).ToUpper();

            return Path.Combine(Database.BlobFolderPath, digestHex + ".blob");
#endif
        }

        /// <summary>
        /// Adds or modifies an attachment by saving a byte array.
        /// </summary>
        /// <param name="name">The attachment name.</param>
        /// <param name="bytes">The attachment data as a byte array.</param>
        /// <param name="contentType">The optional content type.</param>
        /// <exception cref="InvalidOperationException">Thrown if the document is currently read-only.</exception>
        public void SetAttachment(string name, byte[] bytes, string contentType = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentNullException>(bytes != null);

            if (IsReadOnly && ReadOnlyCheck())
            {
                throw new InvalidOperationException(ReadOnlyError);
            }

            ((UnsavedRevision)revision).SetAttachment(name, contentType, bytes);

            if (deletedAttachments != null)
            {
                deletedAttachments.Remove(name);
            }

            if (binderAttachments != null && attachmentTracking.Contains(name))
            {
                var path = Path.Combine(Database.TempFolderPath, $"{Guid.NewGuid()}.blob");

                File.WriteAllBytes(path, bytes);

                // Delete the previous version of the attachment (if one exists).

                AttachmentInfo existing;

                if (binderAttachments.TryGetValue(name, out existing))
                {
                    File.Delete(existing.Path);
                }

                // Update the cached attachment information.

                binderAttachments[name] =
                    new AttachmentInfo()
                    {
                        Name = name,
                        Path = path
                    };

                // Raise the [AttachmentEvent].

                AttachmentEvent?.Invoke(this,
                    new AttachmentEventArgs()
                    {
                        Name  = name,
                        Path  = path
                    });
            }

            OnChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds or modifies an attachment by saving data from a stream.
        /// </summary>
        /// <param name="name">The attachment name.</param>
        /// <param name="input">The attachment data as a stream.</param>
        /// <param name="contentType">The optional content type.</param>
        /// <exception cref="InvalidOperationException">Thrown if the document is currently read-only.</exception>
        /// <remarks>
        /// <note>
        /// This method <b>does not</b> close the <paramref name="input"/> stream.
        /// </note>
        /// </remarks>
        public void SetAttachment(string name, Stream input, string contentType = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentNullException>(input != null);

            if (IsReadOnly && ReadOnlyCheck())
            {
                throw new InvalidOperationException(ReadOnlyError);
            }

            ((UnsavedRevision)revision).SetAttachment(name, contentType, input);

            if (deletedAttachments != null)
            {
                deletedAttachments.Remove(name);
            }

            if (binderAttachments != null && attachmentTracking.Contains(name))
            {
                var path = Path.Combine(Database.TempFolderPath, $"{Guid.NewGuid()}.blob");
                long length;

                using (var output = new FileStream(path, FileMode.CreateNew))
                {
                    input.Position = 0;
                    input.CopyTo(output);
                    length = output.Length;
                    input.Position = 0;     // Need to reset this so Couchbase can persist the
                                            // attachment when the document is saved.
                }

                // Delete the previous version of the attachment (if one exists).

                AttachmentInfo existing;

                if (binderAttachments.TryGetValue(name, out existing))
                {
                    File.Delete(existing.Path);
                }

                // Update the cached attachment information.

                binderAttachments[name] =
                    new AttachmentInfo()
                    {
                        Name = name,
                        Path = path
                    };

                // Raise the [AttachmentEvent].

                AttachmentEvent?.Invoke(this,
                    new AttachmentEventArgs()
                    {
                        Name = name,
                        Path = path
                    });
            }

            OnChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns an existing attachment by name.
        /// </summary>
        /// <param name="name">The attachment name.</param>
        /// <returns>The <see cref="Lite.Attachment"/> if it exists; <c>null</c> otherwise.</returns>
        /// <remarks>
        /// <note>
        /// Be careful to calls <see cref="Attachment.Dispose()"/> when you're done with
        /// an attachment to ensure that underlying file stream will be promptly closed.
        /// </note>
        /// </remarks>
        public Attachment GetAttachment(string name)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));

            // $todo(jeff.lill):
            //
            // Couchbase Lite appears to have a bug where it sometimes returns a
            // non-NULL attachment after its been deleted.  I've reported this
            // as issue #661:
            //
            //      https://github.com/couchbase/couchbase-lite-net/issues/661
            //
            // I'm going to work around this by checking the attachment names
            // first and retuning NULL if the requested name doesn't exist.
            // I also have a unit test that will start failing if Couchbase
            // fixes the issue.  At that time, we can consider removing the
            // extra check.

            if (!revision.AttachmentNames.Contains(name) ||
                (deletedAttachments != null && deletedAttachments.ContainsKey(name)))
            {
                return null;
            }

            return revision.GetAttachment(name);
        }

        /// <summary>
        /// Removes a named attachment, if one exists.
        /// </summary>
        /// <param name="name">The attachment name.</param>
        /// <returns>The <see cref="Lite.Attachment"/> if it exists; <c>null</c> otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document is currently read-only.</exception>
        public void RemoveAttachment(string name)
        {
            if (IsReadOnly && ReadOnlyCheck())
            {
                throw new InvalidOperationException(ReadOnlyError);
            }

            if (binderAttachments != null && attachmentTracking.Contains(name))
            {
                AttachmentInfo attachmentInfo;

                if (binderAttachments.TryGetValue(name, out attachmentInfo))
                {
                    File.Delete(attachmentInfo.Path);
                    binderAttachments.Remove(name);
                }

                AttachmentEvent?.Invoke(this,
                    new AttachmentEventArgs()
                    {
                        Name = name,
                        Path = null
                    });
            }

            ((UnsavedRevision)revision).RemoveAttachment(name);

            if (deletedAttachments == null)
            {
                deletedAttachments = new Dictionary<string, bool>();
            }

            deletedAttachments[name] = true;

            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
