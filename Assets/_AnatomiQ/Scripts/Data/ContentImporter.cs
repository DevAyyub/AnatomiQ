using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace AnatomiQ.Data
{
    /// <summary>
    /// CORE-008 JSON → ScriptableObject parser. JSON is the authoring source of truth (Data Schemas §1);
    /// this turns it into runtime <see cref="OrganAsset"/> / <see cref="DiseaseAsset"/> instances.
    ///
    /// This type performs PARSING only — it does not enforce the Section 9 validation rules
    /// (that is <c>ContentValidator</c>, built in the next chunk) and it does not persist <c>.asset</c>
    /// files (that is the Editor import tool). Both the Editor importer and the Edit Mode tests call
    /// these methods, so the parsing path is shared and identically exercised.
    ///
    /// Design notes:
    /// - A <see cref="ScriptableObject"/> cannot be constructed with <c>new</c>, so each instance is
    ///   created via <see cref="ScriptableObject.CreateInstance{T}()"/> and then filled with
    ///   Newtonsoft <c>Populate</c> (Unity's JsonUtility can't handle the lists/nullable fields here).
    /// - Enums are authored as <c>snake_case</c> strings (e.g. <c>"highlight_pulse"</c>,
    ///   <c>"cardiovascular"</c>, <c>"physiological_state"</c>); a <see cref="StringEnumConverter"/>
    ///   with a <see cref="SnakeCaseNamingStrategy"/> maps them to the PascalCase enum members.
    /// - A malformed item never throws out of these methods: the failure is reported via the
    ///   <c>error</c> out-parameter (single parse) or collected into <c>errors</c> (collection parse)
    ///   and that item is skipped, per the CORE-008 "log, skip, do not crash" fallback.
    ///
    /// This is an edit-time / test utility. Per the CORE-008 load-source decision, the runtime
    /// DataLayer loads pre-imported <c>.asset</c> files rather than calling this at launch.
    /// </summary>
    public static class ContentImporter
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(new SnakeCaseNamingStrategy())
            },
            // Unknown JSON keys are ignored rather than treated as errors; semantic completeness is
            // the validator's job, not the parser's.
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static readonly JsonSerializer _serializer = JsonSerializer.Create(_settings);

        /// <summary>
        /// Parses a single organ JSON object into an <see cref="OrganAsset"/>.
        /// </summary>
        /// <param name="json">A single organ JSON object.</param>
        /// <param name="error">Null on success; the failure reason on failure.</param>
        /// <returns>The parsed asset, or null if the JSON was malformed.</returns>
        public static OrganAsset ParseOrgan(string json, out string error)
            => ParseSingle<OrganAsset>(json, out error);

        /// <summary>
        /// Parses a single disease JSON object into a <see cref="DiseaseAsset"/>.
        /// </summary>
        /// <param name="json">A single disease JSON object.</param>
        /// <param name="error">Null on success; the failure reason on failure.</param>
        /// <returns>The parsed asset, or null if the JSON was malformed.</returns>
        public static DiseaseAsset ParseDisease(string json, out string error)
            => ParseSingle<DiseaseAsset>(json, out error);

        /// <summary>
        /// Parses a collection of organs. Accepts an <c>{ "organs": [ ... ] }</c> envelope, a bare
        /// JSON array, or a single bare object. Malformed entries are skipped and reported in
        /// <paramref name="errors"/>; the returned list contains only the entries that parsed.
        /// </summary>
        public static List<OrganAsset> ParseOrgans(string json, out List<string> errors)
            => ParseCollection<OrganAsset>(json, "organs", out errors);

        /// <summary>
        /// Parses a collection of diseases. Accepts a <c>{ "diseases": [ ... ] }</c> envelope, a bare
        /// JSON array, or a single bare object. Malformed entries are skipped and reported in
        /// <paramref name="errors"/>; the returned list contains only the entries that parsed.
        /// </summary>
        public static List<DiseaseAsset> ParseDiseases(string json, out List<string> errors)
            => ParseCollection<DiseaseAsset>(json, "diseases", out errors);

        private static T ParseSingle<T>(string json, out string error) where T : ScriptableObject
        {
            error = null;
            if (!TryParseToken(json, out var token, out error))
            {
                return null;
            }

            return PopulateFromToken<T>(token, out error);
        }

        private static List<T> ParseCollection<T>(string json, string arrayKey, out List<string> errors)
            where T : ScriptableObject
        {
            errors = new List<string>();
            var results = new List<T>();

            if (!TryParseToken(json, out var root, out var rootError))
            {
                errors.Add(rootError);
                return results;
            }

            var index = 0;
            foreach (var item in ResolveItems(root, arrayKey))
            {
                var parsed = PopulateFromToken<T>(item, out var itemError);
                if (parsed != null)
                {
                    results.Add(parsed);
                }
                else
                {
                    errors.Add($"[{arrayKey}#{index}] {itemError}");
                }

                index++;
            }

            return results;
        }

        private static bool TryParseToken(string json, out JToken token, out string error)
        {
            token = null;
            error = null;
            try
            {
                token = JToken.Parse(json);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Malformed JSON: {ex.Message}";
                return false;
            }
        }

        private static IEnumerable<JToken> ResolveItems(JToken root, string arrayKey)
        {
            if (root is JArray bareArray)
            {
                return bareArray;
            }

            if (root is JObject obj && obj[arrayKey] is JArray envelopedArray)
            {
                return envelopedArray;
            }

            // A single bare object.
            return new[] { root };
        }

        private static T PopulateFromToken<T>(JToken token, out string error) where T : ScriptableObject
        {
            error = null;
            var instance = ScriptableObject.CreateInstance<T>();
            try
            {
                using (var reader = token.CreateReader())
                {
                    _serializer.Populate(reader, instance);
                }

                return instance;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                // Edit-time / test utility: DestroyImmediate is the correct cleanup for a failed
                // instance here (the runtime path loads pre-imported .assets, not this).
                UnityEngine.Object.DestroyImmediate(instance);
                return null;
            }
        }
    }
}
