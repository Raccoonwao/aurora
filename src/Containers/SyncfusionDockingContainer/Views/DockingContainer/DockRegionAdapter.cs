﻿using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using Aurora.Core.Workspace;
using Microsoft.Practices.Prism.Regions;
using Syncfusion.Windows.Tools.Controls;

namespace Aurora.SyncfusionDockingContainer.Views.DockingContainer
{

    public class DockRegionAdapter : RegionAdapterBase<DockingManager>
    {
        private DockingManager manager;
        private DispatcherTimer timer = new DispatcherTimer();
        private bool stateChanged = false;

        public DockRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory)
        {
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += OnTimerTick;
            timer.Start();
           
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer.Stop();
            foreach (Window win in Application.Current.Windows)
            {
                if (win is NativeFloatWindow)
                {
                    win.Close();
                }
            }

        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (stateChanged)
            {
                // Getting all native float window in an current application
                SetIndependentWindows();
                stateChanged = false;
            }
        }

        protected override void Adapt(IRegion region, DockingManager regionTarget)
        {
            this.manager = regionTarget;
            region.Views.CollectionChanged += (sender, e) => OnViewsCollectionChanged(sender, e, region, regionTarget);
            regionTarget.DockStateChanged += (sender, e) => this.OnDockStateChanged(sender, e, region);
            Application.Current.MainWindow.Closing += MainWindow_Closing;

        }

        private void OnDockStateChanged(FrameworkElement sender, DockStateEventArgs e, IRegion region)
        {
            if (e.NewState == DockState.Hidden)
            {
               this.manager.Children.Remove(sender);
                var presenterLayoutDocument = sender as PresenterLayoutDocument;
                if (presenterLayoutDocument != null)
                {
                    region.Remove(presenterLayoutDocument.ViewContext);
                }
            }

            stateChanged = true;
        }

        private static void OnViewsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e, IRegion region,
            DockingManager regionTarget)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    region.Context = regionTarget;
                    foreach (ViewContext item in e.NewItems)
                    {
                        if (item.Info.ViewLocation == null)
                        {
                            item.Info.Title = item.Info.Id;
                            item.Info.ViewLocation = new ViewLocation
                            {
                                DockTarget = "",
                                FloatTarget = "",
                                DockState = DockingState.Document
                            };
                        }

                        var doc = new PresenterLayoutDocument(item) {Name = item.Info.Id};


                        if (doc.ViewLocation.IsFloating)
                        {
                            DockingManager.SetHeader(doc, item.Info?.Title);
                            DockingManager.SetState(doc, DockState.Float);
                            var location = new Rect(doc.ViewLocation.FloatingLeft,
                                doc.ViewLocation.FloatingTop,
                                doc.ViewLocation.FloatingWidth,
                                doc.ViewLocation.FloatingHeight);
                            DockingManager.SetFloatingWindowRect(doc, location);
                            DockingManager.SetSideInDockedMode(doc, (DockSide)doc.ViewLocation.DockSide);
                            DockingManager.SetSideInFloatMode(doc, (DockSide)doc.ViewLocation.DockSide);
                            DockingManager.SetCanFloatMaximize(doc, true);
                            DockingManager.SetFloatWindowState(doc, doc.ViewLocation.Maximized ? WindowState.Maximized : WindowState.Normal);
                            DockingManager.SetTargetNameInFloatingMode(doc, doc.ViewLocation.FloatTarget ?? "");
                            DockingManager.SetTargetNameInDockedMode(doc, doc.ViewLocation.DockTarget ?? "");
                            DockingManager.SetIndexInFloatModeExternally(doc, doc.ViewLocation.DockIndex);
                            DockingManager.SetNoHeader(doc, false);
                            DockingManager.SetDesiredWidthInFloatingMode(doc, doc.ViewLocation.DockWidth);
                            DockingManager.SetDesiredHeightInFloatingMode(doc, doc.ViewLocation.DockHeight);

                        }
                        else
                        {
                            DockingManager.SetHeader(doc, item.Info?.Title);
                            DockingManager.SetState(doc, (DockState)doc.ViewLocation.DockState);
                            DockingManager.SetSideInDockedMode(doc, (DockSide)doc.ViewLocation.DockSide);
                            DockingManager.SetTargetNameInDockedMode(doc, doc.ViewLocation.DockTarget ?? "");
                            DockingManager.SetTargetNameInFloatingMode(doc, doc.ViewLocation.FloatTarget ?? "");
                            DockingManager.SetDesiredWidthInDockedMode(doc, doc.ViewLocation.DockWidth);
                            DockingManager.SetDesiredHeightInDockedMode(doc, doc.ViewLocation.DockHeight);
                            DockingManager.SetIndexInDockMode(doc, doc.ViewLocation.DockIndex);
                            DockingManager.SetCanFloatMaximize(doc, true);
                            DockingManager.SetNoHeader(doc, false);   
                        
                        }
                        regionTarget.Children.Add(doc);
                        TDILayoutPanel.SetTDIIndex(doc, doc.ViewLocation.TabOrderInDocument);
                        DockedElementTabbedHost.SetTabOrderInFloatMode(doc, doc.ViewLocation.TabOrderInFloating);
                        DockedElementTabbedHost.SetTabOrderInDockMode(doc, doc.ViewLocation.TabOrderInDock);

                    }
                    SetIndependentWindows();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (ViewContext item in e.OldItems)
                    {
                        item.View?.Dispose();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override IRegion CreateRegion()
        {
            return new AllActiveRegion();
        }

        private static void SetIndependentWindows()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is NativeFloatWindow)
                {
                    win.Owner = null;
                }
            }
        }
    }
}
