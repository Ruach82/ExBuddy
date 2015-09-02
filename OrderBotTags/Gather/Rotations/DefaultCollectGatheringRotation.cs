namespace ExBuddy.OrderBotTags.Gather.Rotations
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Buddy.Coroutines;

    using ff14bot;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.RemoteWindows;

    // Gathers approx 516 if perception capped.
    [GatheringRotation("DefaultCollect")]
    public class DefaultCollectGatheringRotation : IGatheringRotation
    {
        protected AtkAddonControl MasterpieceWindow;

        protected int CurrentRarity
        {
            get
            {
                if (MasterpieceWindow != null && MasterpieceWindow.IsValid)
                {
                    return Core.Memory.Read<int>(MasterpieceWindow.Pointer + 0x000001C4);    
                }

                return 0;
            }
        }

        protected bool HasDiscerningEye
        {
            get
            {
                return Core.Player.HasAura((int)AbilityAura.DiscerningEye);
            }
        }

        public GatheringRotationAttribute Attributes
        {
            get
            {
                return
                    ReflectionHelper.CustomAttributes<GatheringRotationAttribute>.NotInherited[this.GetType().GUID][0];
            }
        }

        public virtual bool CanOverride
        {
            get
            {
                return true;
            }
        }

        public virtual bool ForceGatherIfMissingGpOrTime
        {
            get
            {
                return Poi.Current != null && Poi.Current.Type == PoiType.Gather
                       && Poi.Current.Name.IndexOf("ephemeral", StringComparison.InvariantCultureIgnoreCase) == -1
                       && Poi.Current.Name.IndexOf("unspoiled", StringComparison.InvariantCultureIgnoreCase) == -1;
            }
        }

        public virtual async Task<bool> Prepare(GatherCollectableTag tag)
        {
            await tag.CastAura(Ability.CollectorsGlove, AbilityAura.CollectorsGlove);

            do
            {
                while (GatheringManager.ShouldPause(DataManager.SpellCache[(uint)Ability.Preparation]))
                {
                    await Coroutine.Yield();
                }

                tag.GatherItem.GatherItem();
            }
            while ((MasterpieceWindow = await GetValidMasterPieceWindow(3000)) == null);

            return true;
        }

        public virtual async Task<bool> ExecuteRotation(GatherCollectableTag tag)
        {
            await DiscerningMethodical(tag);
            await DiscerningMethodical(tag);
            await DiscerningMethodical(tag);

            await IncreaseChance(tag);

            return true;
        }

        public virtual async Task<bool> Gather(GatherCollectableTag tag)
        {
            var exCount = 0;
            int swingsRemaining;
            while ((swingsRemaining = GatheringManager.SwingsRemaining) > 0)
            {
                try
                {
                    swingsRemaining--;
                    await Coroutine.Wait(3000, () => !SelectYesNoItem.IsOpen);
                    while (!SelectYesNoItem.IsOpen && GatheringManager.SwingsRemaining > 0)
                    {
                        if (MasterpieceWindow == null || !MasterpieceWindow.IsValid)
                        {
                            RaptureAtkUnitManager.Update();
                            MasterpieceWindow = await GetValidMasterPieceWindow(3000);
                        }

                        if (MasterpieceWindow != null && MasterpieceWindow.IsValid)
                        {
                            Logging.Write("Clicked Collect");
                            MasterpieceWindow.SendAction(1, 1, 0);
                        }

                        await Coroutine.Wait(3000, () => SelectYesNoItem.IsOpen);
                    }

                    Logging.Write("Clicked Yes");
                    ff14bot.RemoteWindows.SelectYesNoItem.Yes();
                    await Coroutine.Wait(3000, () => !SelectYesNoItem.IsOpen);
                }
                catch (Exception ex)
                {
                    if (++exCount < 3)
                    {
                        Logging.Write("An Exception has occured, but is not yet fatal to our task. Count: " + exCount);
                        Logging.WriteException(ex);
                        Logging.Write("Attempting to continue");
                    }
                    else
                    {
                        TreeRoot.Stop("Too many exceptions");
                        throw;
                    }
                }

                await Coroutine.Wait(3000, () => swingsRemaining == GatheringManager.SwingsRemaining);
            }

            return true;
        }

        public virtual int ShouldOverrideSelectedGatheringRotation(GatherCollectableTag tag)
        {
            if (tag.IsUnspoiled())
            {
                // We need 5 swings to use this rotation
                if (GatheringManager.SwingsRemaining < 5)
                {
                    return -1;
                }
            }

            if (tag.IsEphemeral())
            {
                // We need 4 swings to use this rotation
                if (GatheringManager.SwingsRemaining < 4)
                {
                    return -1;
                }
            }

            // if we have a collectable && the collectable value is greater than or equal to 516: Priority 516
            if (tag.CollectableItem != null && tag.CollectableItem.Value >= 516)
            {
                return 516;
            }

            return -1;
        }

        protected virtual async Task<AtkAddonControl> GetValidMasterPieceWindow(int timeoutMs)
        {
            AtkAddonControl atkControl = null;
            await
                Coroutine.Wait(
                    timeoutMs,
                    () =>
                    (atkControl =
                     RaptureAtkUnitManager.Controls.FirstOrDefault(c => c.Name == "GatheringMasterpiece" && c.IsValid))
                    != null);

            return atkControl;
        }

        protected virtual async Task<bool> IncreaseChance(GatherCollectableTag tag)
        {
            if (Core.Player.CurrentGP >= 50 && tag.GatherItem.Chance < 100)
            {
                if (Core.Player.ClassLevel >= 23 && GatheringManager.SwingsRemaining == 1)
                {
                    // todo: return await flora mastery or clear vision
                }

                return await tag.Cast(Ability.IncreaseGatherChance5);
            }

            return false;
        }

        protected async Task DiscerningMethodical(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.DiscerningEye);
            await tag.Cast(Ability.MethodicalAppraisal);
        }

        protected async Task DiscerningUtmostMethodical(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.DiscerningEye);
            await tag.Cast(Ability.UtmostCaution);
            await tag.Cast(Ability.MethodicalAppraisal);
        }

        protected async Task Impulsive(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.ImpulsiveAppraisal);
        }

        protected async Task Methodical(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.MethodicalAppraisal);
        }

        protected async Task SingleMindMethodical(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.SingleMind);
            await tag.Cast(Ability.MethodicalAppraisal);
        }

        protected async Task SingleMindUtmostMethodical(GatherCollectableTag tag)
        {
            await tag.Cast(Ability.SingleMind);
            await tag.Cast(Ability.UtmostCaution);
            await tag.Cast(Ability.MethodicalAppraisal);
        }
    }
}