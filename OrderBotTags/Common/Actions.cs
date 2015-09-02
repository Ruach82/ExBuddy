namespace ExBuddy.OrderBotTags
{
    using System.Threading.Tasks;

    using Buddy.Coroutines;

    using ff14bot;
    using ff14bot.Managers;

    internal static class Actions
    {
        internal static async Task<bool> CastAura(uint spellId, int delay, int auraId = -1)
        {
            bool result;
            if (auraId == -1 || !Core.Player.HasAura(auraId))
            {
                while (GatheringManager.ShouldPause(DataManager.SpellCache[spellId]))
                {
                    await Coroutine.Yield();
                }
                //await Coroutine.Wait(2500, () => Actionmanager.CanCast(spellId, Core.Player));
                result = Actionmanager.DoAction(spellId, Core.Player);

                //Wait till we have the aura
                await Coroutine.Wait(2500, () => Core.Player.HasAura(auraId));
                //Wait till we can cast again
                await Coroutine.Wait(2500, () => Actionmanager.CanCast(Abilities.Map[Core.Player.CurrentJob][Ability.Preparation], Core.Player));
                await Coroutine.Sleep(delay);
            }
            else
            {
                result = false;
            }

            return result;
        }

        internal static async Task<bool> CastAura(Ability ability, int delay, AbilityAura aura = AbilityAura.None)
        {

            return await CastAura(Abilities.Map[Core.Player.CurrentJob][ability], delay, (int)aura);
        }

        internal static async Task<bool> Cast(uint id, int delay)
        {
            //Wait till we can cast the spell
            while (GatheringManager.ShouldPause(DataManager.SpellCache[id]))
            {
                await Coroutine.Yield();
            }
            //await Coroutine.Wait(2500, () => Actionmanager.CanCast(id, Core.Player));
            var result = Actionmanager.DoAction(id, Core.Player);

            //Wait till we can cast again
            await Coroutine.Wait(2500, () => Actionmanager.CanCast(Abilities.Map[Core.Player.CurrentJob][Ability.Preparation], Core.Player));
            await Coroutine.Sleep(delay);

            return result;
        }

        internal static async Task<bool> Cast(Ability ability, int delay)
        {
            return await Cast(Abilities.Map[Core.Player.CurrentJob][ability], delay);
        }
    }
}