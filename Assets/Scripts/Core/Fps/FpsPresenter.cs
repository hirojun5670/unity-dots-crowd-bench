using R3;
using VContainer.Unity;
using System;
using UnityDotsCrowdLab.Core.UI;

namespace UnityDotsCrowdLab.Core.Fps
{
    public class FpsPresenter : IStartable, IDisposable
    {
        readonly IFpsModel model;
        readonly DOTSView view;
        readonly CompositeDisposable disposable = new();

        public FpsPresenter(IFpsModel model, DOTSView view)
        {
            this.model = model;
            this.view = view;
        }

        public void Start()
        {
            model.CurrentFps
                .CombineLatest(model.AverageFps, model.Mode, (current, average, mode) => (current, average, mode))
                .Subscribe(v => view.SetFpsInfo(v.current, v.average, v.mode))
                .AddTo(disposable);
        }
        public void Dispose() => disposable.Dispose();
    }
}
