using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVBaseDefenseNJ
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static void CreateNewNJTemplates()
        {
            try 
            {
                TacticalItemDef juggHeadDef = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");
                TacticalItemDef juggLegsDef = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Legs_ItemDef");
                TacticalItemDef juggTorsoDef = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");
                TacticalItemDef techHeadDef = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Helmet_BodyPartDef");
                TacticalItemDef rocketLegsDef = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Legs_ItemDef");

                WeaponDef piranhaDef = DefCache.GetDef<WeaponDef>("NJ_PRCR_AssaultRifle_WeaponDef");
                TacticalItemDef piranhaAmmoDef = DefCache.GetDef<TacticalItemDef>("NJ_PRCR_AssaultRifle_AmmoClip_ItemDef");
                //basic template, if New Jericho has not researched Bionics1, will be regular heavies
                TacCharacterDef sourceHeavyTemplateDef = DefCache.GetDef<TacCharacterDef>("NJ_Heavy4_CharacterTemplateDef");


                CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                CustomizationSecondaryColorTagDef mysteryColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_7");

                //second tier will be only heavies with bionic head + legs
                string nameHeavySecondTierDef = "NJ_HeavyBIO1_CharacterTemplateDef";
                TacCharacterDef heavySecondTier = Helper.CreateDefFromClone(sourceHeavyTemplateDef, "{7B4D6E29-0F98-45BB-9F1F-DDDFCFB2ABE0}", nameHeavySecondTierDef);
                heavySecondTier.SpawnCommandId = "HeavyBIO1";
                heavySecondTier.Data.BodypartItems[0] = juggHeadDef;
                heavySecondTier.Data.BodypartItems[1] = juggLegsDef;


                //third tier will be heavies and assaults with rockets legs and technician helmets
                string nameAssaultThirdTierDef = "NJ_AssaultBIO2_CharacterTemplateDef";
                TacCharacterDef sourceAssaultTemplateDef = DefCache.GetDef<TacCharacterDef>("NJ_Assault4_CharacterTemplateDef");
                TacCharacterDef assaultThirdTier = Helper.CreateDefFromClone(sourceAssaultTemplateDef, "{A4FAD12B-69DB-43F8-8975-44148D00BE64}", nameAssaultThirdTierDef);
                assaultThirdTier.SpawnCommandId = "AssaultBIO2";
                assaultThirdTier.Data.BodypartItems[0] = techHeadDef;
                assaultThirdTier.Data.BodypartItems[1] = juggLegsDef;
                assaultThirdTier.Data.BodypartItems[2] = juggTorsoDef;

                assaultThirdTier.Data.EquipmentItems[0] = piranhaDef;
                assaultThirdTier.Data.InventoryItems[0] = piranhaAmmoDef;
              

                List<GameTagDef> gameTagsAssaultThirdTier = assaultThirdTier.Data.GameTags.ToList();
                gameTagsAssaultThirdTier.Add(blackColor);
                gameTagsAssaultThirdTier.Add(mysteryColor);
                
                assaultThirdTier.Data.GameTags = gameTagsAssaultThirdTier.ToArray();

                string nameHeavyThirdTierDef = "NJ_HeavyBIO2_CharacterTemplateDef";
                TacCharacterDef heavyThirdTier = Helper.CreateDefFromClone(sourceHeavyTemplateDef, "{D3C91C73-8949-40C5-BF8F-0E30748A91D0}", nameHeavyThirdTierDef);
                heavyThirdTier.SpawnCommandId = "HeavyBIO2";
                heavyThirdTier.Data.BodypartItems[0] = techHeadDef;
                heavyThirdTier.Data.BodypartItems[1] = juggLegsDef;
                heavyThirdTier.Data.BodypartItems[2] = juggTorsoDef;

                List<GameTagDef> gameTagsheavyThirdTier = heavyThirdTier.Data.GameTags.ToList();
                gameTagsheavyThirdTier.Add(blackColor);
                gameTagsheavyThirdTier.Add(mysteryColor);

                heavyThirdTier.Data.GameTags = gameTagsheavyThirdTier.ToArray();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
