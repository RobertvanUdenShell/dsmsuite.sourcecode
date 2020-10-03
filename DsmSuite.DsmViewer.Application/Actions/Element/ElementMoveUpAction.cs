﻿using DsmSuite.DsmViewer.Application.Actions.Base;
using DsmSuite.DsmViewer.Application.Interfaces;
using DsmSuite.DsmViewer.Model.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DsmSuite.DsmViewer.Application.Actions.Element
{
    public class ElementMoveUpAction : IAction
    {
        private readonly IDsmModel _model;
        private readonly IDsmElement _element;

        public const ActionType RegisteredType = ActionType.ElementMoveUp;

        public ElementMoveUpAction(object[] args)
        {
            if (args.Length == 2)
            {
                _model = args[0] as IDsmModel;
                IReadOnlyDictionary<string, string> data = args[1] as IReadOnlyDictionary<string, string>;
                if ((_model != null) && (data != null))
                {
                    ActionReadOnlyAttributes attributes = new ActionReadOnlyAttributes(_model, data);

                    _element = attributes.GetElement(nameof(_element));
                }
            }
        }

        public ElementMoveUpAction(IDsmModel model, IDsmElement element)
        {
            _model = model;
            _element = element;
        }

        public ActionType Type => RegisteredType;
        public string Title => "Move up element";
        public string Description => $"element={_element.Fullname}";

        public object Do()
        {
            IDsmElement previousElement = _model.PreviousSibling(_element);
            if (previousElement != null)
            {
                _model.Swap(_element, previousElement);
                _model.AssignElementOrder();
            }

            return null;
        }

        public void Undo()
        {
            IDsmElement nextElement = _model.NextSibling(_element);
            if (nextElement != null)
            {
                _model.Swap(nextElement, _element);
                _model.AssignElementOrder();
            }
        }

        public bool IsValid()
        {
            return (_model != null) && 
                   (_element != null);
        }

        public IReadOnlyDictionary<string, string> Data
        {
            get
            {
                ActionAttributes attributes = new ActionAttributes();
                attributes.SetInt(nameof(_element), _element.Id);
                return attributes.Data;
            }
        }
    }
}
