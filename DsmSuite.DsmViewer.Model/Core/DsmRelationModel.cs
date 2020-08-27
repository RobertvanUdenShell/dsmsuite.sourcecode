﻿using DsmSuite.Common.Util;
using DsmSuite.DsmViewer.Model.Interfaces;
using System.Collections.Generic;
using System.Linq;
using DsmSuite.DsmViewer.Model.Persistency;

namespace DsmSuite.DsmViewer.Model.Core
{
    public class DsmRelationModel : IDsmRelationModelFileCallback
    {
        private readonly DsmElementModel _elementsDataModel;
        private readonly Dictionary<int /*relationId*/, DsmRelation> _relationsById;
        private readonly Dictionary<int /*relationId*/, DsmRelation> _deletedRelationsById;
        
        private int _lastRelationId;

        public DsmRelationModel(DsmElementModel elementsDataModel)
        {
            _elementsDataModel = elementsDataModel;
            _elementsDataModel.UnregisterElementRelations += OnUnregisterElementRelations;
            _elementsDataModel.ReregisterElementRelations += OnReregisterElementRelations;

            _elementsDataModel.BeforeElementChangeParent += OnBeforeElementChangeParent;
            _elementsDataModel.AfterElementChangeParent += OnAfterElementChangeParent;

            _relationsById = new Dictionary<int, DsmRelation>();
            _deletedRelationsById = new Dictionary<int, DsmRelation>();
            _lastRelationId = 0;
        }

        public void Clear()
        {
            _relationsById.Clear();
            _deletedRelationsById.Clear();

            _lastRelationId = 0;
        }

        public void ClearHistory()
        {
            _deletedRelationsById.Clear();
        }

        public IDsmRelation ImportRelation(int relationId, int consumerId, int providerId, string type, int weight, bool deleted)
        {
            Logger.LogDataModelMessage("Import relation relationId={relationId} consumerId={consumerId} providerId={providerId} type={type} weight={weight}");

            if (relationId > _lastRelationId)
            {
                _lastRelationId = relationId;
            }

            DsmRelation relation = null;
            if (consumerId != providerId)
            {
                IDsmElement consumer = _elementsDataModel.FindElementById(consumerId);
                IDsmElement provider = _elementsDataModel.FindElementById(providerId);
                if ((consumer != null) && (provider != null))
                {
                    relation = new DsmRelation(relationId, consumer, provider, type, weight) { IsDeleted = deleted };
                    if (deleted)
                    {
                        UnregisterRelation(relation);
                    }
                    else
                    {
                        RegisterRelation(relation);
                    }
                }

            }
            return relation;
        }

        public IDsmRelation AddRelation(int consumerId, int providerId, string type, int weight)
        {
            Logger.LogDataModelMessage("Add relation consumerId={consumerId} providerId={providerId} type={type} weight={weight}");

            DsmRelation relation = null;
            if (consumerId != providerId)
            {
                _lastRelationId++;
                IDsmElement consumer = _elementsDataModel.FindElementById(consumerId);
                IDsmElement provider = _elementsDataModel.FindElementById(providerId);
                if ((consumer != null) && (provider != null))
                {
                    relation = new DsmRelation(_lastRelationId, consumer, provider, type, weight) { IsDeleted = false };
                    RegisterRelation(relation);
                }
            }
            return relation;
        }

        public void ChangeRelationType(IDsmRelation relation, string type)
        {
            DsmRelation changedRelation = relation as DsmRelation;
            if (changedRelation != null)
            {
                UnregisterRelation(changedRelation);

                changedRelation.Type = type;

                RegisterRelation(changedRelation);
            }
        }

        public void ChangeRelationWeight(IDsmRelation relation, int weight)
        {
            DsmRelation changedRelation = relation as DsmRelation;
            if (changedRelation != null)
            {
                UnregisterRelation(changedRelation);

                changedRelation.Weight = weight;

                RegisterRelation(changedRelation);
            }
        }

        public void RemoveRelation(int relationId)
        {
            if (_relationsById.ContainsKey(relationId))
            {
                DsmRelation relation = _relationsById[relationId];
                if (relation != null)
                {
                    UnregisterRelation(relation);
                }
            }
        }

        public void UnremoveRelation(int relationId)
        {
            if (_deletedRelationsById.ContainsKey(relationId))
            {
                DsmRelation relation = _deletedRelationsById[relationId];
                if (relation != null)
                {
                    RegisterRelation(relation);
                }
            }
        }

        public int GetDependencyWeight(IDsmElement consumer, IDsmElement provider)
        {
            int weight = 0;
            DsmElement element = consumer as DsmElement;
            if (element != null)
            {
                weight = element.Dependencies.GetDerivedDependencyWeight(provider);
            }
            return weight;
        }

        public int GetDirectDependencyWeight(IDsmElement consumer, IDsmElement provider)
        {
            int weight = 0;
            DsmElement element = consumer as DsmElement;
            if (element != null)
            {
                weight = element.Dependencies.GetDirectDependencyWeight(provider);
            }
            return weight;
        }

        public CycleType IsCyclicDependency(IDsmElement consumer, IDsmElement provider)
        {
            if ((GetDirectDependencyWeight(consumer, provider) > 0) &&
                (GetDirectDependencyWeight(provider, consumer) > 0))
            {
                return CycleType.System;
            }
            else if ((GetDependencyWeight(consumer, provider) > 0) &&
                     (GetDependencyWeight(provider, consumer) > 0))
            {
                return CycleType.Hierarchical;
            }
            else
            {
                return CycleType.None;
            }
        }

        public DsmRelation GetRelationById(int id)
        {
            return _relationsById.ContainsKey(id) ? _relationsById[id] : null;
        }

        public DsmRelation GetDeletedRelationById(int id)
        {
            return _deletedRelationsById.ContainsKey(id) ? _deletedRelationsById[id] : null;
        }

        public DsmRelation FindRelation(IDsmElement consumer, IDsmElement provider, string type)
        {
            DsmElement e = consumer as DsmElement;
            return e.Dependencies.GetOutgoingRelation(provider, type);
        }

        public IEnumerable<DsmRelation> FindRelations(IDsmElement consumer, IDsmElement provider)
        {
            DsmElement c = consumer as DsmElement;
            IDictionary<int, DsmElement> consumerIds = c.GetElementAndItsChildren();
            DsmElement p = provider as DsmElement;
            IDictionary<int, DsmElement> providerIds = p.GetElementAndItsChildren();

            IList<DsmRelation> relations = new List<DsmRelation>();
            foreach (DsmElement consumerId in consumerIds.Values)
            {
                foreach (DsmElement providerId in providerIds.Values)
                {
                    foreach (DsmRelation relation in consumerId.Dependencies.GetOutgoingRelations(providerId))
                    {
                        if (!relation.IsDeleted)
                        {
                            relations.Add(relation);
                        }
                    }
                }
            }
            return relations;
        }

        public IEnumerable<DsmRelation> FindIngoingRelations(IDsmElement element)
        {
            DsmElement e = element as DsmElement;
            IDictionary<int, DsmElement> elementIds = e.GetElementAndItsChildren();

            List<DsmRelation> relations = new List<DsmRelation>();
            foreach (DsmElement elementId in elementIds.Values)
            {
                foreach (DsmRelation relation in elementId.Dependencies.GetIngoingRelations())
                {
                    if (!elementIds.ContainsKey(relation.Consumer.Id) && !relation.IsDeleted)
                    {
                        relations.Add(relation);
                    }
                }
            }
            return relations;
        }

        public IEnumerable<DsmRelation> FindOutgoingRelations(IDsmElement element)
        {
            DsmElement e = element as DsmElement;
            IDictionary<int, DsmElement> elementIds = e.GetElementAndItsChildren();

            List<DsmRelation> relations = new List<DsmRelation>();
            foreach (DsmElement elementId in elementIds.Values)
            {
                foreach (DsmRelation relation in elementId.Dependencies.GetOutgoingRelations())
                {
                    if (!elementIds.ContainsKey(relation.Provider.Id) && !relation.IsDeleted)
                    {
                        relations.Add(relation);
                    }
                }
            }
            return relations;
        }

        public IEnumerable<DsmRelation> FindInternalRelations(IDsmElement element)
        {
            DsmElement e = element as DsmElement;
            IDictionary<int, DsmElement> elementIds = e.GetElementAndItsChildren();

            List<DsmRelation> relations = new List<DsmRelation>();
            foreach (DsmElement elementId in elementIds.Values)
            {
                foreach (DsmRelation relation in elementId.Dependencies.GetOutgoingRelations())
                {
                    if (elementIds.ContainsKey(relation.Provider.Id) && !relation.IsDeleted)
                    {
                        relations.Add(relation);
                    }
                }
            }
            return relations;
        }

        public IEnumerable<DsmRelation> FindExternalRelations(IDsmElement element)
        {
            DsmElement e = element as DsmElement;
            IDictionary<int, DsmElement> elementIds = e.GetElementAndItsChildren();

            List<DsmRelation> relations = new List<DsmRelation>();
            foreach (DsmElement elementId in elementIds.Values)
            {
                foreach (DsmRelation relation in elementId.Dependencies.GetOutgoingRelations())
                {
                    if (elementIds.ContainsKey(relation.Provider.Id) && !relation.IsDeleted)
                    {
                        relations.Add(relation);
                    }
                }

                foreach (DsmRelation relation in elementId.Dependencies.GetIngoingRelations())
                {
                    if (!elementIds.ContainsKey(relation.Consumer.Id) && !relation.IsDeleted)
                    {
                        relations.Add(relation);
                    }
                }
            }
            return relations;
        }

        public int GetHierarchicalCycleCount(IDsmElement element)
        {
            int cyclesCount = 0;
            CountCycles(element, CycleType.Hierarchical, ref cyclesCount);
            return cyclesCount / 2;
        }

        public int GetSystemCycleCount(IDsmElement element)
        {
            int cyclesCount = 0;
            CountCycles(element, CycleType.System, ref cyclesCount);
            return cyclesCount / 2;
        }

        public IEnumerable<IDsmRelation> GetRelations()
        {
            return _relationsById.Values;
        }

        public int GetRelationCount()
        {
            return _relationsById.Values.Count;
        }

        public IEnumerable<IDsmRelation> GetExportedRelations()
        {
            List<IDsmRelation> exportedRelations = new List<IDsmRelation>();
            exportedRelations.AddRange(_relationsById.Values);
            exportedRelations.AddRange(_deletedRelationsById.Values);
            return exportedRelations.OrderBy(x => x.Id);
        }

        public int GetExportedRelationCount()
        {
            return _relationsById.Values.Count + _deletedRelationsById.Values.Count;
        }

        private void OnUnregisterElementRelations(object sender, IDsmElement element)
        {
            List<DsmRelation> toBeRelationsUnregistered = new List<DsmRelation>();

            foreach (DsmRelation relation in _relationsById.Values)
            {
                if ((element.Id == relation.Consumer.Id) ||
                    (element.Id == relation.Provider.Id))
                {
                    toBeRelationsUnregistered.Add(relation);
                }
            }

            foreach (DsmRelation relation in toBeRelationsUnregistered)
            {
                UnregisterRelation(relation);
            }
        }

        private void OnReregisterElementRelations(object sender, IDsmElement element)
        {
            List<DsmRelation> toBeRelationsReregistered = new List<DsmRelation>();

            foreach (DsmRelation relation in _deletedRelationsById.Values)
            {
                if ((element.Id == relation.Consumer.Id) ||
                    (element.Id == relation.Provider.Id))
                {
                    toBeRelationsReregistered.Add(relation);
                }
            }

            foreach (DsmRelation relation in toBeRelationsReregistered)
            {
                RegisterRelation(relation);
            }
        }

        private void RegisterRelation(DsmRelation relation)
        {
            relation.IsDeleted = false;
            _relationsById[relation.Id] = relation;

            if (_deletedRelationsById.ContainsKey(relation.Id))
            {
                _deletedRelationsById.Remove(relation.Id);
            }

            DsmElement consumer = relation.Consumer as DsmElement;
            consumer.Dependencies.AddOutgoingRelation(relation);

            DsmElement provider = relation.Provider as DsmElement;
            provider.Dependencies.AddIngoingRelation(relation);

            AddWeights(relation);
        }

        private void UnregisterRelation(DsmRelation relation)
        {
            relation.IsDeleted = true;
            _relationsById.Remove(relation.Id);

            _deletedRelationsById[relation.Id] = relation;

            DsmElement consumer = relation.Consumer as DsmElement;
            consumer.Dependencies.RemoveOutgoingRelation(relation);

            DsmElement provider = relation.Provider as DsmElement;
            provider.Dependencies.RemoveIngoingRelation(relation);

            RemoveWeights(relation);
        }

        private void OnBeforeElementChangeParent(object sender, IDsmElement element)
        {
            foreach (IDsmRelation relation in FindExternalRelations(element))
            {
                RemoveWeights(relation);
            }
        }

        private void OnAfterElementChangeParent(object sender, IDsmElement element)
        {
            foreach (IDsmRelation relation in FindExternalRelations(element))
            {
                AddWeights(relation);
            }
        }

        private void AddWeights(IDsmRelation relation)
        {
            DsmElement currentConsumer = relation.Consumer as DsmElement;
            while (currentConsumer != null)
            {
                IDsmElement currentProvider = relation.Provider;
                while (currentProvider != null)
                {
                    currentConsumer.Dependencies.AddDerivedWeight(currentProvider, relation.Weight);
                    currentProvider = currentProvider.Parent;
                }
                currentConsumer = currentConsumer.Parent as DsmElement;
            }
        }

        private void RemoveWeights(IDsmRelation relation)
        {
            DsmElement currentConsumer = relation.Consumer as DsmElement;
            while (currentConsumer != null)
            {
                IDsmElement currentProvider = relation.Provider;
                while (currentProvider != null)
                {
                    currentConsumer.Dependencies.RemoveDerivedWeight(currentProvider, relation.Weight);
                    currentProvider = currentProvider.Parent;
                }
                currentConsumer = currentConsumer.Parent as DsmElement; ;
            }
        }

        private void CountCycles(IDsmElement element, CycleType cycleType, ref int cycleCount)
        {
            foreach (IDsmElement consumer in element.Children)
            {
                foreach (IDsmElement provider in element.Children)
                {
                    if (IsCyclicDependency(consumer, provider) == cycleType)
                    {
                        cycleCount++;
                    }
                }
            }

            foreach (IDsmElement child in element.Children)
            {
                CountCycles(child, cycleType, ref cycleCount);
            }
        }
    }
}
