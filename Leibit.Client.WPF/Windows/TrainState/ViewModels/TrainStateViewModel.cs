﻿using Leibit.BLL;
using Leibit.Client.WPF.ViewModels;
using Leibit.Core.Client.Commands;
using Leibit.Entities;
using Leibit.Entities.Common;
using Leibit.Entities.LiveData;
using System.Linq;
using System.Windows;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace Leibit.Client.WPF.Windows.TrainState.ViewModels
{
    public class TrainStateViewModel : ChildWindowViewModelBase
    {

        #region - Needs -
        private Area m_Area;
        private LiveDataBLL m_LiveDataBll;
        #endregion

        #region - Ctor -
        public TrainStateViewModel(Area area, int? trainNumber)
        {
            m_LiveDataBll = new LiveDataBLL();
            SaveCommand = new CommandHandler(__Save, false);
            m_Area = area;
            TrainNumber = trainNumber;
        }
        #endregion

        #region - Properties -

        #region [TrainNumber]
        public int? TrainNumber
        {
            get => Get<int?>();
            set
            {
                Set(value);
                __Initialize();
            }
        }
        #endregion

        #region [CurrentSchedule]
        private LiveSchedule CurrentSchedule
        {
            get => Get<LiveSchedule>();
            set
            {
                Set(value);
                OnPropertyChanged(nameof(StationName));
                OnPropertyChanged(nameof(TrackName));
                OnPropertyChanged(nameof(CanCompose));
                OnPropertyChanged(nameof(CanPrepare));
                OnPropertyChanged(nameof(CanRevoke));
            }
        }
        #endregion

        #region [StationName]
        public string StationName
        {
            get
            {
                if (CurrentSchedule == null)
                    return string.Empty;

                return $"{CurrentSchedule.Schedule.Station.Name} ({CurrentSchedule.Schedule.Station.ShortSymbol})";
            }
        }
        #endregion

        #region [TrackName]
        public string TrackName => CurrentSchedule?.LiveTrack?.Name;
        #endregion

        #region [CanCompose]
        public bool CanCompose
        {
            get
            {
                if (CurrentSchedule == null)
                    return false;
                if (CurrentSchedule.IsComposed)
                    return false;

                return CurrentSchedule.Train.BlockHistory.Select(b => b.Track.Station).Distinct().Count() == 1
                    && CurrentSchedule.Train.Block?.Track?.CalculateDelay == true
                    && CurrentSchedule.Train.Block?.Track?.IsPlatform == true;
                ;
            }
        }
        #endregion

        #region [CanPrepare]
        public bool CanPrepare
        {
            get => !CurrentSchedule?.IsPrepared ?? false;
        }
        #endregion

        #region [CanRevoke]
        public bool CanRevoke
        {
            get => CurrentSchedule != null && (CurrentSchedule.IsComposed || CurrentSchedule.IsPrepared);
        }
        #endregion

        #region [TypeIsComposed]
        public bool TypeIsComposed
        {
            get => Get<bool>();
            set => Set(value);
        }
        #endregion

        #region [TypeIsPrepared]
        public bool TypeIsPrepared
        {
            get => Get<bool>();
            set => Set(value);
        }
        #endregion

        #region [TypeRevocation]
        public bool TypeRevocation
        {
            get => Get<bool>();
            set => Set(value);
        }
        #endregion

        #region [SaveCommand]
        public CommandHandler SaveCommand { get; }
        #endregion

        #endregion

        #region - Private methods -

        #region [__Initialize]
        private void __Initialize()
        {
            if (TrainNumber.HasValue && m_Area.LiveTrains.ContainsKey(TrainNumber.Value))
                CurrentSchedule = m_Area.LiveTrains[TrainNumber.Value].Schedules.FirstOrDefault(s => s.IsArrived && !s.IsDeparted);
            else
                CurrentSchedule = null;

            TypeIsComposed = false;
            TypeIsPrepared = false;
            TypeRevocation = false;

            if (CanCompose)
                TypeIsComposed = true;
            else if (CanPrepare)
                TypeIsPrepared = true;
            else if (CanRevoke)
                TypeRevocation = true;

            SaveCommand.SetCanExecute(CurrentSchedule != null);
        }
        #endregion

        #region [__Save]
        private void __Save()
        {
            if (CurrentSchedule == null)
                return;

            var state = TypeIsComposed ? eTrainState.Composed :
                        TypeIsPrepared ? eTrainState.Prepared :
                        eTrainState.None;

            var result = m_LiveDataBll.SetTrainState(CurrentSchedule, state);

            if (result.Succeeded)
            {
                if (TypeIsComposed)
                    OnStatusBarTextChanged($"Zug {TrainNumber} in {CurrentSchedule.Schedule.Station.ShortSymbol} bereitgestellt gemeldet");
                else if (TypeIsPrepared)
                    OnStatusBarTextChanged($"Zug {TrainNumber} in {CurrentSchedule.Schedule.Station.ShortSymbol} vorbereitet gemeldet");
                else if (TypeRevocation)
                    OnStatusBarTextChanged($"Zugvorbereitungsmeldung für Zug {TrainNumber} in {CurrentSchedule.Schedule.Station.ShortSymbol} zurückgenommen");

                OnRefresh();
                OnCloseWindow();
            }
            else
                MessageBox.Show(result.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        #endregion

    }
}
