﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Umbraco.Core.Cache;
using Umbraco.Core.Exceptions;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Core.Manifest
{
    /// <summary>
    /// Parses the Main.js file and replaces all tokens accordingly.
    /// </summary>
    public class ManifestParser
    {
        private static readonly string Utf8Preamble = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

        private readonly IRuntimeCacheProvider _cache;
        private readonly ILogger _logger;
        private readonly ManifestValidatorCollection _validators;

        private string _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestParser"/> class.
        /// </summary>
        public ManifestParser(IRuntimeCacheProvider cache, ManifestValidatorCollection validators, ILogger logger)
            : this(cache, validators, "~/App_Plugins", logger)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestParser"/> class.
        /// </summary>
        private ManifestParser(IRuntimeCacheProvider cache, ManifestValidatorCollection validators, string path, ILogger logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullOrEmptyException(nameof(path));
            Path = path;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Path
        {
            get => _path;
            set => _path = value.StartsWith("~/") ? IOHelper.MapPath(value) : value;
        }

        /// <summary>
        /// Gets all manifests, merged into a single manifest object.
        /// </summary>
        /// <returns></returns>
        public PackageManifest Manifest
            => _cache.GetCacheItem<PackageManifest>("Umbraco.Core.Manifest.ManifestParser::Manifests", () =>
            {
                var manifests = GetManifests();
                return MergeManifests(manifests);
            });

        /// <summary>
        /// Gets all manifests.
        /// </summary>
        private IEnumerable<PackageManifest> GetManifests()
        {
            var manifests = new List<PackageManifest>();

            foreach (var path in GetManifestFiles())
            {
                try
                {
                    var text = File.ReadAllText(path);
                    text = TrimPreamble(text);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                    var manifest = ParseManifest(text);
                    manifests.Add(manifest);
                }
                catch (Exception e)
                {
                    _logger.Error<ManifestParser>($"Failed to parse manifest at \"{path}\", ignoring.", e);
                }
            }

            return manifests;
        }

        /// <summary>
        /// Merges all manifests into one.
        /// </summary>
        private static PackageManifest MergeManifests(IEnumerable<PackageManifest> manifests)
        {
            var scripts = new HashSet<string>();
            var stylesheets = new HashSet<string>();
            var propertyEditors = new List<PropertyEditor>();
            var parameterEditors = new List<ParameterEditor>();
            var gridEditors = new List<GridEditor>();

            foreach (var manifest in manifests)
            {
                if (manifest.Scripts != null) foreach (var script in manifest.Scripts) scripts.Add(script);
                if (manifest.Stylesheets != null) foreach (var stylesheet in manifest.Stylesheets) stylesheets.Add(stylesheet);
                if (manifest.PropertyEditors != null) propertyEditors.AddRange(manifest.PropertyEditors);
                if (manifest.ParameterEditors != null) parameterEditors.AddRange(manifest.ParameterEditors);
                if (manifest.GridEditors != null) gridEditors.AddRange(manifest.GridEditors);
            }

            return new PackageManifest
            {
                Scripts = scripts.ToArray(),
                Stylesheets = stylesheets.ToArray(),
                PropertyEditors = propertyEditors.ToArray(),
                ParameterEditors = parameterEditors.ToArray(),
                GridEditors = gridEditors.ToArray()
            };
        }

        // gets all manifest files (recursively)
        private IEnumerable<string> GetManifestFiles()
            => Directory.GetFiles(_path, "package.manifest", SearchOption.AllDirectories);

        private static string TrimPreamble(string text)
        {
            // strangely StartsWith(preamble) would always return true
            if (text.Substring(0, 1) == Utf8Preamble)
                text = text.Remove(0, Utf8Preamble.Length);

            return text;
        }

        /// <summary>
        /// Parses a manifest.
        /// </summary>
        internal PackageManifest ParseManifest(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullOrEmptyException(nameof(text));

            var manifest = JsonConvert.DeserializeObject<PackageManifest>(text,
                new PropertyEditorConverter(_logger),
                new ParameterEditorConverter(),
                new ManifestValidatorConverter(_validators));

            // scripts and stylesheets are raw string, must process here
            for (var i = 0; i < manifest.Scripts.Length; i++)
                manifest.Scripts[i] = IOHelper.ResolveVirtualUrl(manifest.Scripts[i]);
            for (var i = 0; i < manifest.Stylesheets.Length; i++)
                manifest.Stylesheets[i] = IOHelper.ResolveVirtualUrl(manifest.Stylesheets[i]);

            return manifest;
        }

        // purely for tests
        internal IEnumerable<GridEditor> ParseGridEditors(string text)
        {
            return JsonConvert.DeserializeObject<IEnumerable<GridEditor>>(text);
        }
    }
}
