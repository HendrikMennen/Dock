﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dock.Model.Controls;

namespace Dock.Model.Factories
{
    /// <summary>
    /// Dock factory base.
    /// </summary>
    public abstract class BaseDockFactory : IDockFactory
    {
        /// <inheritdoc/>
        public virtual IDictionary<string, Func<object>> ContextLocator { get; set; }

        /// <inheritdoc/>
        public virtual IDictionary<string, Func<IDockHost>> HostLocator { get; set; }

        /// <inheritdoc/>
        public virtual object GetContext(string id, object context)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Func<object> locator = null;
                if (ContextLocator?.TryGetValue(id, out locator) == true)
                {
                    return locator?.Invoke();
                }
            }
            return context;
        }

        /// <inheritdoc/>
        public virtual IDockHost GetHost(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Func<IDockHost> locator = null;
                if (HostLocator?.TryGetValue(id, out locator) == true)
                {
                    return locator?.Invoke();
                }
            }
            throw new Exception($"Dock host with {id} is not registered.");
        }

        /// <inheritdoc/>
        public virtual void Update(IDockWindow window, object context, IDock owner)
        {
            window.Host = GetHost(window.Id);
            window.Context = GetContext(window.Id, context);
            window.Owner = owner;
            window.Factory = this;

            if (window.Layout != null)
            {
                Update(window.Layout, context, null);
            }
        }

        /// <inheritdoc/>
        public virtual void Update(IList<IDockWindow> windows, object context, IDock owner)
        {
            foreach (var window in windows)
            {
                Update(window, context, owner);
            }
        }

        /// <inheritdoc/>
        public virtual void Update(IDock view, object context, IDock parent)
        {
            view.Context = GetContext(view.Id, context);
            view.Parent = parent;
            view.Factory = this;

            if (view.Windows != null)
            {
                Update(view.Windows, context, view);
            }

            if (view.Views != null)
            {
                Update(view.Views, context, view);
            }
        }

        /// <inheritdoc/>
        public virtual void Update(IList<IDock> views, object context, IDock parent)
        {
            foreach (var view in views)
            {
                Update(view, context, parent);
            }
        }

        /// <inheritdoc/>
        public virtual IDock FindRootLayout(IDock dock)
        {
            if (dock.Parent == null)
            {
                return dock;
            }
            return FindRootLayout(dock.Parent);
        }

        /// <inheritdoc/>
        public virtual void Remove(IDock dock)
        {
            if (dock?.Parent is IDock parent)
            {
                parent.Views?.Remove(dock);
            }
        }

        /// <inheritdoc/>
        public virtual void RemoveView(IDock dock, int index)
        {
            dock.Views.RemoveAt(index);

            if (dock.Views.Count > 0)
            {
                dock.CurrentView = dock.Views[index > 0 ? index - 1 : 0];
            }
            else
            {
                dock.CurrentView = null;
            }
        }

        /// <inheritdoc/>
        public virtual void MoveView(IDock dock, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < targetIndex)
            {
                var item = dock.Views[sourceIndex];
                dock.Views.RemoveAt(sourceIndex);
                dock.Views.Insert(targetIndex, item);
                dock.CurrentView = item;
            }
            else
            {
                int removeIndex = sourceIndex;
                if (dock.Views.Count > removeIndex)
                {
                    var item = dock.Views[sourceIndex];
                    dock.Views.RemoveAt(removeIndex);
                    dock.Views.Insert(targetIndex, item);
                    dock.CurrentView = item;
                }
            }
        }

        /// <inheritdoc/>
        public virtual void MoveView(IDock sourceDock, IDock targetDock, int sourceIndex, int targetIndex)
        {
            var item = sourceDock.Views[sourceIndex];
            sourceDock.Views.RemoveAt(sourceIndex);

            if (targetDock.Views == null)
            {
                targetDock.Views = new ObservableCollection<IDock>();
            }
            targetDock.Views.Insert(targetIndex, item);

            if (item.Factory is IDockFactory factory)
            {
                factory.Update(item, item.Context, targetDock);
            }

            if (sourceDock.Views.Count > 0)
            {
                sourceDock.CurrentView = sourceDock.Views[sourceIndex > 0 ? sourceIndex - 1 : 0];
            }
            else
            {
                sourceDock.CurrentView = null;
            }

            if (targetDock.Views.Count > 0)
            {
                targetDock.CurrentView = targetDock.Views[targetIndex];
            }
        }

        /// <inheritdoc/>
        public virtual void MoveTo(IDock dock, IDock parent)
        {
            IDock orignalParent = dock.Parent;
            int index = orignalParent.Views.IndexOf(dock);

            orignalParent.Views.Remove(dock);
            parent.Views.Add(dock);

            Update(dock, dock.Context, parent);

            if (orignalParent.Views.Count > 0)
            {
                orignalParent.CurrentView = orignalParent.Views[index > 0 ? index - 1 : 0];
            }
            else
            {
                orignalParent.CurrentView = null;
            }

            parent.CurrentView = dock;
        }

        /// <inheritdoc/>
        public virtual void Swap(IDock first, IDock second)
        {
            IDock firstParent = first.Parent;
            int firstIndex = firstParent.Views.IndexOf(first);

            IDock secondParent = second.Parent;
            int secondIndex = secondParent.Views.IndexOf(second);

            firstParent.Views.RemoveAt(firstIndex);
            secondParent.Views.RemoveAt(secondIndex);

            firstParent.Views.Insert(firstIndex, second);
            secondParent.Views.Insert(secondIndex, first);

            Update(first, first.Context, secondParent);
            Update(second, second.Context, firstParent);

            firstParent.CurrentView = second;
            secondParent.CurrentView = first;
        }

        /// <inheritdoc/>
        public virtual void SwapView(IDock dock, int sourceIndex, int targetIndex)
        {
            var item1 = dock.Views[sourceIndex];
            var item2 = dock.Views[targetIndex];
            dock.Views[targetIndex] = item1;
            dock.Views[sourceIndex] = item2;
            dock.CurrentView = item2;
        }

        /// <inheritdoc/>
        public virtual void SwapView(IDock sourceDock, IDock targetDock, int sourceIndex, int targetIndex)
        {
            var item1 = sourceDock.Views[sourceIndex];
            var item2 = targetDock.Views[targetIndex];
            sourceDock.Views[sourceIndex] = item2;
            targetDock.Views[targetIndex] = item1;

            if (item1.Factory is IDockFactory factory1)
            {
                factory1.Update(item1, item1.Context, targetDock);
            }

            if (item2.Factory is IDockFactory factory2)
            {
                factory2.Update(item2, item2.Context, sourceDock);
            }

            sourceDock.CurrentView = item2;
            targetDock.CurrentView = item1;
        }

        /// <inheritdoc/>
        public virtual void Replace(IDock source, IDock destination)
        {
            if (source.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(source);
                parent.Views.RemoveAt(index);
                parent.Views.Insert(index, destination);
            }
        }

        /// <inheritdoc/>
        public virtual IDock CreateSplitLayout(IDock dock, IDock view, object context, DockOperation operation)
        {
            double width = double.NaN;
            double height = double.NaN;
            string originalDock = dock.Dock;
            double originalWidth = dock.Width;
            double originalHeight = dock.Height;

            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Right:
                    width = originalWidth == double.NaN ? double.NaN : originalWidth / 2.0;
                    height = dock.Height == double.NaN ? double.NaN : dock.Height;
                    break;
                case DockOperation.Top:
                case DockOperation.Bottom:
                    width = originalWidth == double.NaN ? double.NaN : originalWidth;
                    height = originalHeight == double.NaN ? double.NaN : originalHeight / 2.0;
                    break;
            }

            if (view != null)
            {
                view.Dock = "";
                view.Width = double.NaN;
                view.Height = double.NaN;
            }

            IDock split = new LayoutDock
            {
                Id = nameof(LayoutDock),
                Title = nameof(LayoutDock),
                Width = width,
                Height = height,
                CurrentView = view ?? null,
                Views = view == null ? null : new ObservableCollection<IDock> { view }
            };

            switch (operation)
            {
                case DockOperation.Left:
                    split.Dock = "Left";
                    split.Width = width;
                    dock.Dock = "Right";
                    dock.Width = width;
                    break;
                case DockOperation.Right:
                    split.Dock = "Right";
                    split.Width = width;
                    dock.Dock = "Left";
                    dock.Width = width;
                    break;
                case DockOperation.Top:
                    split.Dock = "Top";
                    split.Height = height;
                    dock.Dock = "Bottom";
                    dock.Height = height;
                    break;
                case DockOperation.Bottom:
                    split.Dock = "Bottom";
                    split.Height = height;
                    dock.Dock = "Top";
                    dock.Height = height;
                    break;
            }

            var layout = new LayoutDock
            {
                Id = nameof(LayoutDock),
                Dock = originalDock,
                Width = originalWidth,
                Height = originalHeight,
                Title = nameof(LayoutDock),
                CurrentView = null,
                Views = new ObservableCollection<IDock>
                {
                    (dock.Dock == "Left" || dock.Dock == "Top") ? dock : split,
                    new SplitterDock()
                    {
                        Id = nameof(SplitterDock),
                        Title = nameof(SplitterDock),
                        Dock = (split.Dock == "Left" || split.Dock == "Right") ? "Left" : "Top",
                        Width = double.NaN,
                        Height = double.NaN,
                    },
                    (dock.Dock == "Left" || dock.Dock == "Top") ? split : dock,
                }
            };

            return layout;
        }

        /// <inheritdoc/>
        public virtual void Split(IDock dock, IDock view, DockOperation operation)
        {
            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Right:
                case DockOperation.Top:
                case DockOperation.Bottom:
                    {
                        var layout = CreateSplitLayout(dock, view, dock.Context, operation);

                        Replace(dock, layout);
                        Update(layout, dock.Context, dock.Parent);
                    }
                    break;
                default:
                    throw new NotSupportedException($"Not support dock operation: {operation}.");
            }
        }

        /// <inheritdoc/>
        public virtual void SplitToFill(IDock dock)
        {
            // TODO:
        }

        /// <inheritdoc/>
        public virtual void SplitToLeft(IDock dock)
        {
            Split(dock, null, DockOperation.Left);
        }

        /// <inheritdoc/>
        public virtual void SplitToRight(IDock dock)
        {
            Split(dock, null, DockOperation.Right);
        }

        /// <inheritdoc/>
        public virtual void SplitToTop(IDock dock)
        {
            Split(dock, null, DockOperation.Top);
        }

        /// <inheritdoc/>
        public virtual void SplitToBottom(IDock dock)
        {
            Split(dock, null, DockOperation.Bottom);
        }

        /// <inheritdoc/>
        public virtual void SplitToWindow(IDock dock)
        {
            // TODO:
        }

        private void InsertLayout(IDock dock, int index, object context)
        {
            var layout = new LayoutDock
            {
                Id = nameof(LayoutDock),
                Title = nameof(LayoutDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(layout, context, dock);

            dock.Views.Insert(index, layout);
        }

        private void InsertRoot(IDock dock, int index, object context)
        {
            var root = new RootDock
            {
                Id = nameof(RootDock),
                Title = nameof(RootDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(root, context, dock);

            dock.Views.Insert(index, root);
        }

        private void InsertSplitter(IDock dock, int index, object context)
        {
            var splitter = new SplitterDock
            {
                Id = nameof(SplitterDock),
                Title = nameof(SplitterDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(splitter, context, dock);

            dock.Views.Insert(index, splitter);
        }

        private void InsertDocument(IDock dock, int index, object context)
        {
            var document = new DocumentDock
            {
                Id = nameof(ToolDock),
                Title = nameof(ToolDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(document, context, dock);

            dock.Views.Insert(index, document);
        }

        private void InsertTool(IDock dock, int index, object context)
        {
            var tool = new ToolDock
            {
                Id = nameof(ToolDock),
                Title = nameof(ToolDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(tool, context, dock);

            dock.Views.Insert(index, tool);
        }

        private void InsertView(IDock dock, int index, object context)
        {
            var view = new ViewDock
            {
                Id = nameof(ViewDock),
                Title = nameof(ViewDock),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(view, context, dock);

            dock.Views.Insert(index, view);
        }

        private void InsertToolTab(IDock dock, int index, object context)
        {
            var toolTab = new ToolTab
            {
                Id = nameof(ToolTab),
                Title = nameof(ToolTab),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(toolTab, context, dock);

            dock.Views.Insert(index, toolTab);
        }

        private void InsertDocumentTab(IDock dock, int index, object context)
        {
            var documentTab = new DocumentTab
            {
                Id = nameof(DocumentTab),
                Title = nameof(DocumentTab),
                Width = double.NaN,
                Height = double.NaN
            };

            Update(documentTab, context, dock);

            dock.Views.Insert(index, documentTab);
        }

        /// <inheritdoc/>
        public virtual void AddLayout(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertLayout(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddRoot(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertRoot(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddSplitter(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertSplitter(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddDocument(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertDocument(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddTool(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertTool(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddView(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertView(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddToolTab(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertToolTab(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void AddDocumentTab(IDock dock)
        {
            if (dock.Views == null)
            {
                dock.Views = new ObservableCollection<IDock>();
            }
            InsertDocumentTab(dock, dock.Views.Count, dock.Context);
        }

        /// <inheritdoc/>
        public virtual void InsertLayoutBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertLayout(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertRootBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertRoot(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertSplitterBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertSplitter(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertDocumentBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertDocument(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertToolBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertTool(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertViewBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertView(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertToolTabBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertToolTab(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertDocumentTabBefore(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock);
                InsertDocumentTab(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertLayoutAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertLayout(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertRootAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertRoot(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertSplitterAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertSplitter(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertDocumentAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertDocument(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertToolAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertTool(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertViewAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertView(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertToolTabAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertToolTab(parent, index, parent.Context);
            }
        }

        /// <inheritdoc/>
        public virtual void InsertDocumentTabAfter(IDock dock)
        {
            if (dock.Parent is IDock parent)
            {
                int index = parent.Views.IndexOf(dock) + 1;
                InsertDocumentTab(parent, index, parent.Context);
            }
        }

        /// <summary>
        /// Copies properties from source to destination dock.
        /// </summary>
        /// <param name="source">The source dock.</param>
        /// <param name="destination">The destination dock.</param>
        /// <param name="bCopyViews">The flag indicating whether to copy source views.</param>
        /// <param name="bCopyWindows">The flag indicating whether to copy source windows.</param>
        private void Copy(IDock source, IDock destination, bool bCopyViews, bool bCopyWindows)
        {
            destination.Id = source.Id;
            destination.Dock = source.Dock;
            destination.Width = source.Width;
            destination.Height = source.Height;
            destination.Title = source.Title;

            if (bCopyViews)
            {
                destination.Views = source.Views;
                destination.CurrentView = source.CurrentView;
                destination.DefaultView = source.DefaultView;
            }

            if (bCopyWindows)
            {
                destination.Windows = source.Windows;
            }
        }

        /// <inheritdoc/>
        public virtual void ConvertToLayout(IDock dock)
        {
            var layout = new LayoutDock();
            Copy(dock, layout, true, true);
            Update(layout, dock.Context, dock.Parent);
            Replace(dock, layout);
        }

        /// <inheritdoc/>
        public virtual void ConvertToRoot(IDock dock)
        {
            var root = new RootDock();
            Copy(dock, root, true, true);
            Update(root, dock.Context, dock.Parent);
            Replace(dock, root);
        }

        /// <inheritdoc/>
        public virtual void ConvertToSplitter(IDock dock)
        {
            var splitter = new SplitterDock();
            Copy(dock, splitter, false, false);
            Update(splitter, dock.Context, dock.Parent);
            Replace(dock, splitter);
        }

        /// <inheritdoc/>
        public virtual void ConvertToDocument(IDock dock)
        {
            var document = new DocumentDock();
            Copy(dock, document, true, false);
            Update(document, dock.Context, dock.Parent);
            Replace(dock, document);
        }

        /// <inheritdoc/>
        public virtual void ConvertToTool(IDock dock)
        {
            var tool = new ToolDock();
            Copy(dock, tool, true, false);
            Update(tool, dock.Context, dock.Parent);
            Replace(dock, tool);
        }

        /// <inheritdoc/>
        public virtual void ConvertToView(IDock dock)
        {
            var view = new ViewDock();
            Copy(dock, view, false, true);
            Update(view, dock.Context, dock.Parent);
            Replace(dock, view);
        }

        /// <inheritdoc/>
        public virtual void ConvertToToolTab(IDock dock)
        {
            var toolTab = new ToolTab();
            Copy(dock, toolTab, false, true);
            Update(toolTab, dock.Context, dock.Parent);
            Replace(dock, toolTab);
        }

        /// <inheritdoc/>
        public virtual void ConvertToDocumentTab(IDock dock)
        {
            var documentTab = new DocumentTab();
            Copy(dock, documentTab, false, true);
            Update(documentTab, dock.Context, dock.Parent);
            Replace(dock, documentTab);
        }

        /// <inheritdoc/>
        public virtual IDockWindow CreateWindowFrom(IDock source)
        {
            IDock target = null;

            switch (source)
            {
                case IRootDock targetRoot:
                    {
                        target = targetRoot.CurrentView;
                    }
                    break;
                case IViewDock targetView:
                    {
                        target = new LayoutDock
                        {
                            Id = nameof(LayoutDock),
                            Title = nameof(LayoutDock),
                            CurrentView = source,
                            Views = new ObservableCollection<IDock> { source }
                        };
                    }
                    break;
                case IToolTab targetToolTab:
                    {
                        target = new ToolDock
                        {
                            Id = nameof(ToolDock),
                            Title = nameof(ToolDock),
                            CurrentView = source,
                            Views = new ObservableCollection<IDock> { source }
                        };
                    }
                    break;
                case IDocumentTab targetDocumentTab:
                    {
                        target = new DocumentDock
                        {
                            Id = nameof(DocumentDock),
                            Title = nameof(DocumentDock),
                            CurrentView = source,
                            Views = new ObservableCollection<IDock> { source }
                        };
                    }
                    break;
                case ILayoutDock targetLayout:
                    {
                        target = targetLayout;

                    }
                    break;
                case IToolDock targetTool:
                    {
                        target = source;
                    }
                    break;
                case IDocumentDock targetDocument:
                    {
                        target = source;
                    }
                    break;
                default:
                    {
                        Console.WriteLine($"Not supported window source: {source}");
                        return null;
                    }
            }

            target.Dock = "";
            target.Width = double.NaN;
            target.Height = double.NaN;

            var root = new RootDock
            {
                Id = nameof(RootDock),
                Title = nameof(RootDock),
                Width = double.NaN,
                Height = double.NaN,
                CurrentView = target,
                DefaultView = target,
                Views = new ObservableCollection<IDock> { target }
            };

            var window = new DockWindow()
            {
                Id = nameof(DockWindow),
                Title = nameof(DockWindow),
                Width = double.NaN,
                Height = double.NaN,
                Layout = root
            };

            return window;
        }

        /// <inheritdoc/>
        public virtual void AddWindow(IDock owner, IDockWindow window, object context)
        {
            if (owner.Windows == null)
            {
                owner.Windows = new ObservableCollection<IDockWindow>();
            }

            owner.Windows?.Add(window);

            Update(window, context, owner);
        }

        /// <inheritdoc/>
        public virtual void RemoveWindow(IDockWindow window)
        {
            if (window?.Owner is IDock dock)
            {
                window.Destroy();
                dock.Windows?.Remove(window);
            }
        }

        /// <inheritdoc/>
        public abstract IDock CreateLayout();

        /// <inheritdoc/>
        public abstract void InitLayout(IDock layout, object context);
    }
}
