
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

       /* public void Initialize()
        {
            DateTime start = DateTime.Now;
            _defNameToGuidCache = new Dictionary<string, List<string>>();
            foreach (BaseDef baseDef in _repo.DefRepositoryDef.AllDefs)
            {
                AddDef(baseDef.name, baseDef.Guid);
            }
            DateTime end = DateTime.Now;
           // TFTVLogger.Always($"DefCache initialised, took {end.Millisecond - start.Millisecond} ms ({end.Ticks - start.Ticks} ticks) to gather all defs into the cache, number of cached defs: {_defNameToGuidCache.Count}");
        }*/

        public T GetDef<T>(string name) where T : BaseDef
        {
            string guid = _defNameToGuidCache[name].FirstOrDefault();
            return guid != default ? (T)_repo.GetDef(guid) : null;
        }

        public List<T> GetDefs<T>(string name) where T : BaseDef
        {
            if (_defNameToGuidCache.ContainsKey(name))
            {
                return _defNameToGuidCache[name].Select(guid => (T)_repo.GetDef(guid)).ToList();
            }
            return null;
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
