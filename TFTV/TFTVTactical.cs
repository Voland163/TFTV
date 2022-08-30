using Base.Core;
using Base.Serialization.General;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;


namespace TFTV
{
    /// <summary>
    /// Mod's custom save data for tactical.
    /// </summary>
    [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll)]
    public class TFTVTacInstanceData
    {
        // public int ExampleData;
        // Dictionary to transfer the characters geoscape stamina to tactical level by actor ID
        public List<int> charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs;
        //VO#3 is WP cost +50%
        public bool VoidOmen3Active = TFTVVoidOmens.VoidOmen3Active;
        //VO#5 is haven defenders hostil, needed for victory kludge
        public bool VoidOmen5Active = TFTVVoidOmens.VoidOmen5Active;
        //VO#7 is more mist in missions
        public bool VoidOmen7Active = TFTVVoidOmens.VoidOmen7Active;
        //VO#10 is no limit to Delirium
        public bool VoidOmen10Active = TFTVVoidOmens.VoidOmen10Active;
        //VO#15 is more Umbra
        public bool VoidOmen15Active = TFTVVoidOmens.VoidOmen15Active;
        //VO#16 is Umbras can appear anywhere and attack anyone
        public bool VoidOmen16Active = TFTVVoidOmens.VoidOmen16Active;
        //Check if Umbra can be spawned in tactical
        public bool UmbraResearched = TFTVUmbra.UmbraResearched;
        public Dictionary<string, int> DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
    //    public TimeUnit timeRevenantLastSeenSaveData = TFTVRevenant.timeRevenantLastSeen;
    //    public TimeSpan timeLastRevenantSpawned = TFTVRevenant.timeLastRevenantSpawned;
        public List<string> revenantSpecialResistance = TFTVRevenant.revenantSpecialResistance;
        public bool revenantSpawned = TFTVRevenant.revenantSpawned;
        public bool revenantCanSpawnSaveDate = TFTVRevenant.revenantCanSpawn;
    }

    /// <summary>
    /// Represents a mod instance specific for Tactical game.
    /// Each time Tactical level is loaded, new mod's ModTactical is created. 
    /// </summary>
    public class TFTVTactical : ModTactical
    {
        /// <summary>
        /// Called when Tactical starts.
        /// </summary>
        public override void OnTacticalStart()
        {
            /// Tactical level controller is accessible at any time.
            TacticalLevelController tacController = Controller;
            /// ModMain is accesible at any time
            TFTVMain main = (TFTVMain)Main;
            //TFTV give Dtony's Delirium Perks
            //  TFTVDelirium.DeliriumPerksOnTactical(tacController);
            TFTVRevenant.CreateRevenantDefs();
            TFTVRevenant.ModifyRevenantResistanceAbility(Controller);
            TFTVRevenant.RevenantCheckAndSpawn(Controller);
           
        }

        /// <summary>
        /// Called when Tactical ends.
        /// </summary>
        public override void OnTacticalEnd()
        {
            base.OnTacticalEnd();

          /*  if (TFTVRevenant.revenantSpawned)
            {
                TFTVLogger.Always("revenant Spawned is true and " + TFTVRevenant.timeLastRevenantSpawned.DateTime);
                TFTVRevenant.timeLastRevenantSpawned = TFTVRevenant.timeOfMissionStart;
                TFTVLogger.Always("revenant Spawned is true and " + TFTVRevenant.timeLastRevenantSpawned.DateTime);
            }*/
            /*  if (TFTVRevenant.DeadSoldiersDelirium.Count != 0 && TFTVRevenant.DeadSoldiersDelirium.Count > TFTVRevenant.GeoDeadSoldiersDelirium.Count)
              {
                  TFTVRevenant.DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium;
              }*/

            
        }
        /// <summary>
        /// Called when Tactical save is being process. At this point level is already created, but TacticalStart is not called.
        /// </summary>
        /// <param name="data">Instance data serialized for this mod. Cannot be null.</param>
        public override void ProcessTacticalInstanceData(object instanceData)
        {
            TFTVTacInstanceData data = (TFTVTacInstanceData)instanceData;
            TFTVStamina.charactersWithBrokenLimbs = data.charactersWithBrokenLimbs;
            TFTVVoidOmens.VoidOmen3Active = data.VoidOmen3Active;
            TFTVVoidOmens.VoidOmen5Active = data.VoidOmen5Active;
            TFTVVoidOmens.VoidOmen7Active = data.VoidOmen7Active;
            TFTVVoidOmens.VoidOmen10Active = data.VoidOmen10Active;
            TFTVVoidOmens.VoidOmen15Active = data.VoidOmen15Active;
            TFTVVoidOmens.VoidOmen16Active = data.VoidOmen16Active;
            TFTVUmbra.UmbraResearched = data.UmbraResearched;
            TFTVRevenant.DeadSoldiersDelirium = data.DeadSoldiersDelirium;
          //  TFTVRevenant.timeRevenantLastSeen = data.timeRevenantLastSeenSaveData;
           // TFTVRevenant.timeLastRevenantSpawned = data.timeLastRevenantSpawned;
            TFTVRevenant.revenantSpawned = data.revenantSpawned;
            TFTVRevenant.revenantSpecialResistance = data.revenantSpecialResistance;
            TFTVRevenant.revenantCanSpawn = data.revenantCanSpawnSaveDate;
        }
        /// <summary>
        /// Called when Tactical save is going to be generated, giving mod option for custom save data.
        /// </summary>
        /// <returns>Object to serialize or null if not used.</returns>
        public override object RecordTacticalInstanceData()
        {
            return new TFTVTacInstanceData()
            {
                charactersWithBrokenLimbs = TFTVStamina.charactersWithBrokenLimbs,
                VoidOmen3Active = TFTVVoidOmens.VoidOmen3Active,
                VoidOmen5Active = TFTVVoidOmens.VoidOmen5Active,
                VoidOmen7Active = TFTVVoidOmens.VoidOmen7Active,
                VoidOmen10Active = TFTVVoidOmens.VoidOmen10Active,
                VoidOmen15Active = TFTVVoidOmens.VoidOmen15Active,
                VoidOmen16Active = TFTVVoidOmens.VoidOmen16Active,
                UmbraResearched = TFTVUmbra.UmbraResearched,
                DeadSoldiersDelirium = TFTVRevenant.DeadSoldiersDelirium,
             //   timeRevenantLastSeenSaveData = TFTVRevenant.timeRevenantLastSeen,
                revenantSpawned = TFTVRevenant.revenantSpawned,
                revenantSpecialResistance = TFTVRevenant.revenantSpecialResistance,
                revenantCanSpawnSaveDate = TFTVRevenant.revenantCanSpawn,
              //  timeLastRevenantSpawned = TFTVRevenant.timeLastRevenantSpawned,
            };
        }
        /// <summary>
        /// Called when new turn starts in tactical. At this point all factions must play in their order.
        /// </summary>
        /// <param name="turnNumber">Current turn number</param>
        public override void OnNewTurn(int turnNumber)
        {
            TFTVRevenant.revenantSpecialResistance.Clear();
            TFTVUmbra.SpawnUmbra(Controller);

        }
    }
}