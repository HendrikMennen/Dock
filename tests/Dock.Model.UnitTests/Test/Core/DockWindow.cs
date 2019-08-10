﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dock.Model
{
    public class DockWindow : IDockWindow
    {
        private IHostAdapter _hostAdapter;

        public string Id { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public bool Topmost { get; set; }

        public string Title { get; set; }

        public IView Owner { get; set; }

        public IDockFactory Factory { get; set; }

        public IDock Layout { get; set; }

        public IDockHost Host { get; set; }

        public DockWindow()
        {
            _hostAdapter = new HostAdapter(this);
        }

        public void Save()
        {
            _hostAdapter.Save();
        }

        public void Present(bool isDialog)
        {
            _hostAdapter.Present(isDialog);
        }

        public void Exit()
        {
            _hostAdapter.Exit();
        }
    }
}