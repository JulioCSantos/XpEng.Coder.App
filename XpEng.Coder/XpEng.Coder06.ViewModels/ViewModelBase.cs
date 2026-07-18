using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder09.Models;

namespace XpEng.Coder06.ViewModels {
    public abstract class ViewModelBase : ObservableObject{
        protected MainModel MainModel => MainModel.Instance;
    }
}
