﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Umbraco.Core.Models.PublishedContent
{
    /// <summary>
    /// Represents an <see cref="IPublishedContent"/> type.
    /// </summary>
    /// <remarks>Instances of the <see cref="PublishedContentType"/> class are immutable, ie
    /// if the content type changes, then a new class needs to be created.</remarks>
    public class PublishedContentType
    {
        private readonly PublishedPropertyType[] _propertyTypes;

        // fast alias-to-index xref containing both the raw alias and its lowercase version
        private readonly Dictionary<string, int> _indexes = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishedContentType"/> class with a content type.
        /// </summary>
        public PublishedContentType(IContentTypeComposition contentType, IPublishedContentTypeFactory factory)
            : this(contentType.Id, contentType.Alias, contentType.GetItemType(), contentType.CompositionAliases(), contentType.Variations)
        {
            var propertyTypes = contentType.CompositionPropertyTypes
                .Select(x => factory.CreatePropertyType(this, x))
                .ToList();

            if (ItemType == PublishedItemType.Member)
                EnsureMemberProperties(propertyTypes, factory);

            _propertyTypes = propertyTypes.ToArray();

            InitializeIndexes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishedContentType"/> with specific values.
        /// </summary>
        /// <remarks>
        /// <para>This constructor is for tests and is not intended to be used directly from application code.</para>
        /// <para>Values are assumed to be consisted and are not checked.</para>
        /// </remarks>
        public PublishedContentType(int id, string alias, PublishedItemType itemType, IEnumerable<string> compositionAliases, IEnumerable<PublishedPropertyType> propertyTypes, ContentVariation variations)
            : this (id, alias, itemType, compositionAliases, variations)
        {
            var propertyTypesA = propertyTypes.ToArray();
            foreach (var propertyType in propertyTypesA)
                propertyType.ContentType = this;
            _propertyTypes = propertyTypesA;

            InitializeIndexes();
        }

        private PublishedContentType(int id, string alias, PublishedItemType itemType, IEnumerable<string> compositionAliases, ContentVariation variations)
        {
            Id = id;
            Alias = alias;
            ItemType = itemType;
            CompositionAliases = new HashSet<string>(compositionAliases, StringComparer.InvariantCultureIgnoreCase);
            Variations = variations;
        }

        private void InitializeIndexes()
        {
            for (var i = 0; i < _propertyTypes.Length; i++)
            {
                var propertyType = _propertyTypes[i];
                _indexes[propertyType.Alias] = i;
                _indexes[propertyType.Alias.ToLowerInvariant()] = i;
            }
        }

        // Members have properties such as IMember LastLoginDate which are plain C# properties and not content
        // properties; they are exposed as pseudo content properties, as long as a content property with the
        // same alias does not exist already.
        private void EnsureMemberProperties(List<PublishedPropertyType> propertyTypes, IPublishedContentTypeFactory factory)
        {
            var aliases = new HashSet<string>(propertyTypes.Select(x => x.Alias), StringComparer.OrdinalIgnoreCase);

            foreach ((var alias, (var dataTypeId, var editorAlias)) in BuiltinMemberProperties)
            {
                if (aliases.Contains(alias)) continue;
                propertyTypes.Add(factory.CreatePropertyType(this, alias, dataTypeId, editorAlias, ContentVariation.InvariantNeutral));
            }
        }

        // fixme - this list somehow also exists in constants, see memberTypeRepository => remove duplicate!
        private static readonly Dictionary<string, (int, string)> BuiltinMemberProperties = new Dictionary<string, (int, string)>
        {
            { "Email", (Constants.DataTypes.Textbox, Constants.PropertyEditors.TextboxAlias) },
            { "Username", (Constants.DataTypes.Textbox, Constants.PropertyEditors.TextboxAlias) },
            { "PasswordQuestion", (Constants.DataTypes.Textbox, Constants.PropertyEditors.TextboxAlias) },
            { "Comments", (Constants.DataTypes.Textbox, Constants.PropertyEditors.TextboxAlias) },
            { "IsApproved", (Constants.DataTypes.Boolean, Constants.PropertyEditors.BooleanAlias) },
            { "IsLockedOut", (Constants.DataTypes.Boolean, Constants.PropertyEditors.BooleanAlias) },
            { "LastLockoutDate", (Constants.DataTypes.Datetime, Constants.PropertyEditors.DateTimeAlias) },
            { "CreateDate", (Constants.DataTypes.Datetime, Constants.PropertyEditors.DateTimeAlias) },
            { "LastLoginDate", (Constants.DataTypes.Datetime, Constants.PropertyEditors.DateTimeAlias) },
            { "LastPasswordChangeDate", (Constants.DataTypes.Datetime, Constants.PropertyEditors.DateTimeAlias) },
        };

        #region Content type

        /// <summary>
        /// Gets the content type identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the content type alias.
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Gets the content item type.
        /// </summary>
        public PublishedItemType ItemType { get; }

        /// <summary>
        /// Gets the aliases of the content types participating in the composition.
        /// </summary>
        public HashSet<string> CompositionAliases { get; }

        /// <summary>
        /// Gets the content variations of the content type.
        /// </summary>
        public ContentVariation Variations { get; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the content type properties.
        /// </summary>
        public IEnumerable<PublishedPropertyType> PropertyTypes => _propertyTypes;

        /// <summary>
        /// Gets a property type index.
        /// </summary>
        /// <remarks>The alias is case-insensitive. This is the only place where alias strings are compared.</remarks>
        public int GetPropertyIndex(string alias)
        {
            if (_indexes.TryGetValue(alias, out var index)) return index; // fastest
            if (_indexes.TryGetValue(alias.ToLowerInvariant(), out index)) return index; // slower
            return -1;
        }

        // virtual for unit tests - fixme explain
        /// <summary>
        /// Gets a property type.
        /// </summary>
        public virtual PublishedPropertyType GetPropertyType(string alias)
        {
            var index = GetPropertyIndex(alias);
            return GetPropertyType(index);
        }

        // virtual for unit tests - fixme explain
        /// <summary>
        /// Gets a property type.
        /// </summary>
        public virtual PublishedPropertyType GetPropertyType(int index)
        {
            return index >= 0 && index < _propertyTypes.Length ? _propertyTypes[index] : null;
        }

        #endregion
    }
}
