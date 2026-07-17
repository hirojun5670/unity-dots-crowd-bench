
using R3;
using UnityEngine;
using System;
using System.Diagnostics;
using UnityDotsCrowdLab.Core.UI;
using VContainer.Unity;

namespace UnityDotsCrowdLab.Features.SpawnStatus
{
    public class SpawnStatusPresenter : IStartable, IDisposable
    {
        readonly ISpawnStatustModel model;
        readonly DOTSView view;
        readonly CompositeDisposable disposable = new();

        public SpawnStatusPresenter(ISpawnStatustModel model, DOTSView view)
        {
            this.model = model;
            this.view = view;
        }

        public void Start()
        {
            model.Count.Subscribe(value =>
            {
                view.SetCount(value);
            })
            .AddTo(disposable);
        }

        public void Dispose() => disposable.Dispose();
    }
}