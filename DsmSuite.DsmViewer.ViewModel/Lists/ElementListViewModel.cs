﻿using System.Collections.Generic;
using DsmSuite.DsmViewer.Model;
using DsmSuite.DsmViewer.Model.Interfaces;
using DsmSuite.DsmViewer.ViewModel.Common;

namespace DsmSuite.DsmViewer.ViewModel.Lists
{
    public class ElementListViewModel : ViewModelBase
    {
        public ElementListViewModel(string title, IEnumerable<IDsmElement> elements)
        {
            Title = title;

            var elementViewModels = new List<ElementListItemViewModel>();

            int index = 1;
            foreach (IDsmElement element in elements)
            {
                elementViewModels.Add(new ElementListItemViewModel(index, element));
                index++;
            }

            Elements = elementViewModels;
        }

        public string Title { get; }

        public List<ElementListItemViewModel> Elements { get;  }
    }
}
