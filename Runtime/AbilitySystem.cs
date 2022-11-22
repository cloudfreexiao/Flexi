using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Physalia.AbilityFramework
{
    public class AbilitySystem
    {
        public event Action<IEventContext> EventReceived;
        public event Action<IChoiceContext> ChoiceOccurred;

        private readonly StatOwnerRepository ownerRepository;
        private readonly AbilityRunner runner;
        private readonly AbilityEventQueue eventQueue = new();

        private readonly Dictionary<int, string> graphTable = new();

        private IEnumerable<Actor> overridedIteratorGetter;

        internal AbilitySystem(StatDefinitionListAsset statDefinitionListAsset, AbilityRunner runner)
        {
            ownerRepository = StatOwnerRepository.Create(statDefinitionListAsset);
            this.runner = runner;
        }

        public StatOwner CreateOwner()
        {
            return ownerRepository.CreateOwner();
        }

        public void RemoveOwner(StatOwner owner)
        {
            ownerRepository.RemoveOwner(owner);
        }

        public StatOwner GetOwner(int id)
        {
            return ownerRepository.GetOwner(id);
        }

        public void LoadAbilityGraph(int id, string graphJson)
        {
            bool success = graphTable.TryAdd(id, graphJson);
            if (!success)
            {
                Logger.Error($"[{nameof(AbilitySystem)}] Load graph failed! Already exists graph with Id:{id}");
            }
        }

        public void LoadAbilityGraph(int id, AbilityGraphAsset graphAsset)
        {
            bool success = graphTable.TryAdd(id, graphAsset.Text);
            if (!success)
            {
                Logger.Error($"[{nameof(AbilitySystem)}] Load graph failed! Already exists graph with Id:{id}");
            }
        }

        public AbilityInstance GetAbilityInstance(int id)
        {
            bool success = graphTable.TryGetValue(id, out string graphJson);
            if (!success)
            {
                Logger.Error($"[{nameof(AbilitySystem)}] Get instance failed! Not exists graph with Id:{id}");
                return null;
            }

            AbilityGraph graph = JsonConvert.DeserializeObject<AbilityGraph>(graphJson);
            AbilityInstance instance = new AbilityInstance(id, this, graph);
            return instance;
        }

        public AbilityInstance AppendAbility(Actor actor, int abilityId)
        {
            return AppendAbility(actor.Owner, abilityId);
        }

        public AbilityInstance AppendAbility(StatOwner owner, int abilityId)
        {
            AbilityInstance abilityInstance = GetAbilityInstance(abilityId);
            if (abilityInstance == null)
            {
                return null;
            }

            abilityInstance.SetOwner(owner);
            owner.AppendAbility(abilityInstance);
            return abilityInstance;
        }

        public void RemoveAbility(StatOwner owner, int abilityId)
        {
            owner.RemoveAbility(abilityId);
        }

        public void ClearAllAbilities(StatOwner owner)
        {
            owner.ClearAllAbilities();
        }

        public void OverrideIterator(IEnumerable<Actor> iterator)
        {
            overridedIteratorGetter = iterator;
        }

        internal void EnqueueEvent(IEventContext eventContext)
        {
            eventQueue.Enqueue(eventContext);
            EventReceived?.Invoke(eventContext);
        }

        internal void TriggerCachedEvents()
        {
            if (eventQueue.Count == 0)
            {
                return;
            }

            runner.PushNewAbilityQueue();
            while (eventQueue.Count > 0)
            {
                IEventContext eventContext = eventQueue.Dequeue();
                IterateAbilitiesFromStatOwners(eventContext);
            }
            runner.PopEmptyQueues();
        }

        private void IterateAbilitiesFromStatOwners(IEventContext eventContext)
        {
            if (overridedIteratorGetter != null)
            {
                IEnumerator<Actor> enumerator = overridedIteratorGetter.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Actor actor = enumerator.Current;
                    EnqueueAbilityIfAble(actor.Owner, eventContext);
                }
            }
            else
            {
                foreach (StatOwner owner in ownerRepository.Owners)
                {
                    EnqueueAbilityIfAble(owner, eventContext);
                }
            }
        }

        private void EnqueueAbilityIfAble(StatOwner owner, IEventContext eventContext)
        {
            foreach (AbilityInstance ability in owner.Abilities)
            {
                if (ability.CanExecute(eventContext))
                {
                    EnqueueAbility(ability, eventContext);
                }
            }
        }

        public void EnqueueAbilityAndRun(AbilityInstance instance, IEventContext eventContext)
        {
            EnqueueAbility(instance, eventContext);
            Run();
        }

        public void EnqueueAbility(AbilityInstance instance, IEventContext eventContext)
        {
            instance.Reset();
            instance.SetPayload(eventContext);
            runner.Add(instance);
        }

        public void Run()
        {
            runner.Run(this);
        }

        public void Resume(IResumeContext resumeContext)
        {
            runner.Resume(this, resumeContext);
        }

        public void RefreshStatsAndModifiers()
        {
            ownerRepository.RefreshStatsForAllOwners();
            RefreshModifiers();
        }

        public void RefreshModifiers()
        {
            var payload = new StatRefreshEvent();
            foreach (StatOwner owner in ownerRepository.Owners)
            {
                foreach (AbilityInstance ability in owner.Abilities)
                {
                    if (ability.CanExecute(payload))
                    {
                        ability.SetPayload(payload);
                        ability.Execute();
                    }
                }
            }

            ownerRepository.RefreshStatsForAllOwners();
        }

        internal void TriggerChoice(IChoiceContext context)
        {
            ChoiceOccurred?.Invoke(context);
        }
    }
}
