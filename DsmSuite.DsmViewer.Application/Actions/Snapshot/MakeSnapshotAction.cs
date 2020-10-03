﻿using DsmSuite.DsmViewer.Application.Actions.Base;
using DsmSuite.DsmViewer.Application.Interfaces;
using DsmSuite.DsmViewer.Model.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DsmSuite.DsmViewer.Application.Actions.Snapshot
{
    public class MakeSnapshotAction : IAction
    {
        private readonly IDsmModel _model;
        private readonly string _name;

        public const ActionType RegisteredType = ActionType.Snapshot;

        public MakeSnapshotAction(object[] args)
        {
            if (args.Length == 2)
            {
                _model = args[0] as IDsmModel;
                IReadOnlyDictionary<string, string> data = args[1] as IReadOnlyDictionary<string, string>;

                if ((_model != null) && (data != null))
                {
                    ActionReadOnlyAttributes attributes = new ActionReadOnlyAttributes(_model, data);

                    _name = attributes.GetString(nameof(_name));
                }
            }
        }

        public MakeSnapshotAction(IDsmModel model, string name)
        {
            _name = name;
        }
        
        public ActionType Type => RegisteredType;
        public string Title => "Make snapshot";
        public string Description => $"name={_name}";

        public object Do()
        {
            return null;
        }

        public void Undo()
        {
        }

        public bool IsValid()
        {
            return (_model != null) && 
                   (_name != null);
        }

        public IReadOnlyDictionary<string, string> Data
        {
            get
            {
                ActionAttributes attributes = new ActionAttributes();
                attributes.SetString(nameof(_name), _name);
                return attributes.Data;
            }
        }
    }
}
