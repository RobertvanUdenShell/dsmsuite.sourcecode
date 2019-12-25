﻿using DsmSuite.DsmViewer.Model.Interfaces;
using System;
using System.Collections.Generic;

namespace DsmSuite.DsmViewer.Application.Sorting
{
    public class SortAlgorithmFactory
    {
        private static Dictionary<string, Type> _algorithms;

        static SortAlgorithmFactory()
        {
            _algorithms = new Dictionary<string, Type>();
            RegisterAlgorithmTypes();
        }

        public static void RegisterAlgorithm(string name, Type algorithm)
        {
            _algorithms[name] = algorithm;
        }

        public static ISortAlgorithm CreateAlgorithm(IDsmModel model, IDsmElement element, string algorithName)
        {
            ISortAlgorithm algoritm = null;

            if (_algorithms.ContainsKey(algorithName))
            {
                Type type = _algorithms[algorithName];
                object[] args = { model , element };
                object argumentList = args;
                algoritm = Activator.CreateInstance(type, argumentList) as ISortAlgorithm;
            }
            return algoritm;
        }

        public static IEnumerable<string> GetSupportedAlgorithms()
        {
            return _algorithms.Keys;
        }

        private static void RegisterAlgorithmTypes()
        {
            RegisterAlgorithm(PartitionSortAlgorithm.AlgorithmName, typeof(PartitionSortAlgorithm));
            RegisterAlgorithm(AlphabeticalSortAlgorithm.AlgorithmName, typeof(AlphabeticalSortAlgorithm));
        }
    }
}
