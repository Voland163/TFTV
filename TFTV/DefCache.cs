using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Entities.GameTags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    public class DefCache
    {
        public DefCache()
        {
            _defNameToGuidCache = new Dictionary<string, List<string>>();
            foreach (BaseDef baseDef in _repo.DefRepositoryDef.AllDefs)
            {
                AddDef(baseDef.name, baseDef.Guid);
            }

            //  Initialize();
        }

        public T GetDef<T>(string name) where T : BaseDef
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                if (!_defNameToGuidCache.TryGetValue(name, out List<string> guids) || guids == null || guids.Count == 0)
                {
                    return null;
                }

                // Prefer the first GUID that is actually of the requested type.
                foreach (string guid in guids)
                {
                    if (string.IsNullOrEmpty(guid))
                    {
                        continue;
                    }

                    BaseDef def = _repo.GetDef(guid);
                    if (def is T typed)
                    {
                        return typed;
                    }
                }

                // Diagnostics: we found the name, but nothing matched the expected type.
                string types = string.Join(", ", guids
                    .Select(g =>
                    {
                        BaseDef d = string.IsNullOrEmpty(g) ? null : _repo.GetDef(g);
                        return d == null ? "<null>" : d.GetType().Name;
                    })
                    .Distinct());

                TFTVLogger.Always($"[DefCache] GetDef<{typeof(T).Name}>('{name}') failed. Cached GUID count: {guids.Count}. Types found: {types}.");

                return null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        public List<T> GetDefs<T>(string name) where T : BaseDef
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                if (!_defNameToGuidCache.TryGetValue(name, out List<string> guids) || guids == null)
                {
                    return null;
                }

                List<T> result = new List<T>();
                foreach (string guid in guids)
                {
                    BaseDef def = string.IsNullOrEmpty(guid) ? null : _repo.GetDef(guid);
                    if (def is T typed)
                    {
                        result.Add(typed);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        public void AddDef(string name, string guid)
        {
            if (_defNameToGuidCache.ContainsKey(name))
            {
                if (!_defNameToGuidCache[name].Contains(guid))
                {
                    _defNameToGuidCache[name].Add(guid);
                }
            }
            else
            {
                _defNameToGuidCache.Add(name, new List<string> { guid });
            }
        }

        private readonly DefRepository _repo = GameUtl.GameComponent<DefRepository>();
        private Dictionary<string, List<string>> _defNameToGuidCache;
    }
}
