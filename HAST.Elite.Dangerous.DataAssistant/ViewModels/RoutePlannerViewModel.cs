﻿// ***********************************************************************
// Assembly         : HAST.Elite.Dangerous.DataAssistant
// Author           : Jon Benson
// Created          : 07-01-2015
// 
// Last Modified By : Jon Benson
// Last Modified On : 15-01-2015
// ***********************************************************************
// <copyright file="RoutePlannerViewModel.cs" company="Jon Benson">
//     Copyright (c) Jon Benson. All rights reserved.
// </copyright>
// ***********************************************************************
// 

namespace HAST.Elite.Dangerous.DataAssistant.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;

    using HAST.Elite.Dangerous.DataAssistant.Properties;
    using HAST.Elite.Dangerous.DataAssistant.Routing;

    using log4net;

    /// <summary>Class RoutePlannerViewModel.</summary>
    public class RoutePlannerViewModel : ObservableObject, IDisposable
    {
        #region Static Fields

        private static readonly ILog Log = LogManager.GetLogger(typeof(RoutePlannerViewModel));

        #endregion

        #region Fields

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" /> planner
        /// </summary>
        internal readonly RoutePlannerAStar RoutePlanner = new RoutePlannerAStar();

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.destination" />
        /// </summary>
        internal string destination;

        /// <summary>The jump range</summary>
        internal float jumpRange;

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.source" />
        /// </summary>
        internal string source;

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" />
        /// </summary>
        private readonly ObservableCollection<IRouteNode> route = new ObservableCollection<IRouteNode>();

        /// <summary>
        ///     <para>
        ///         The calculate <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" />
        ///     </para>
        ///     <para>command</para>
        /// </summary>
        private RelayCommand calculateRouteCommand;

        /// <summary>The calculation time</summary>
        private TimeSpan calculationTime;

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.disposed" />
        /// </summary>
        private bool disposed;

        private double distance;

        private bool isEconomySelected;

        private int numberOfJumps;

        private IRouteNode selectedRouteNode;

        /// <summary>The swap systems command</summary>
        private RelayCommand swapSystemsCommand;

        /// <summary>
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.timeout" />
        /// </summary>
        private TimeSpan timeout;

        private bool wasRouteFound;

        #endregion

        #region Constructors and Destructors

        /// <summary>Finalizes an instance of the <see cref="RoutePlannerViewModel" /> class.</summary>
        ~RoutePlannerViewModel()
        {
            this.Dispose(false);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     <para>
        ///         Gets the calculate <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" />
        ///     </para>
        ///     <para>command.</para>
        /// </summary>
        public ICommand CalculateRouteCommand
        {
            get
            {
                return this.calculateRouteCommand
                       ?? (this.calculateRouteCommand =
                           new RelayCommand(
                               this.CalculateRoute,
                               () =>
                               !string.IsNullOrWhiteSpace(this.Source) && !string.IsNullOrWhiteSpace(this.Destination)));
            }
        }

        /// <summary>Gets the calculation time.</summary>
        public TimeSpan CalculationTime
        {
            get
            {
                return this.calculationTime;
            }
            private set
            {
                this.Set(ref this.calculationTime, value);
            }
        }

        /// <summary>Gets or sets the destination.</summary>
        /// <exception cref="UnknownSystemException">Thrown if the systems is not found in the database.</exception>
        public string Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.RoutePlanner.Destination = value;
                if (this.Set(ref this.destination, value))
                {
                    this.CalculateRoute();
                }
            }
        }

        /// <summary>Gets or sets the distance.</summary>
        public double Distance
        {
            get
            {
                return this.distance;
            }
            set
            {
                this.Set(ref this.distance, value);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is using economical <see cref="route" /> planning.
        /// </summary>
        public bool IsEconomySelected
        {
            get
            {
                return this.isEconomySelected;
            }
            set
            {
                if (this.Set(ref this.isEconomySelected, value))
                {
                    this.RoutePlanner.CurrentAlgorithm = value
                                                             ? RoutePlannerAStar.Algorithm.Economy
                                                             : RoutePlannerAStar.Algorithm.LeastJumps;
                    this.CalculateRoute();
                }
            }
        }

        /// <summary>Gets or sets the jump range.</summary>
        public float JumpRange
        {
            get
            {
                return this.jumpRange;
            }
            set
            {
                if (this.Set(ref this.jumpRange, value))
                {
                    this.RoutePlanner.JumpRange = value;
                    this.CalculateRoute();
                }
            }
        }

        /// <summary>Gets or sets the number of jumps.</summary>
        public int NumberOfJumps
        {
            get
            {
                return this.numberOfJumps;
            }
            set
            {
                this.Set(ref this.numberOfJumps, value);
            }
        }

        /// <summary>Gets or sets the route.</summary>
        public ObservableCollection<IRouteNode> Route
        {
            get
            {
                return this.route;
            }
        }

        /// <summary>
        ///     Gets or sets the selected
        ///     <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" /> node.
        /// </summary>
        public IRouteNode SelectedRouteNode
        {
            get
            {
                return this.selectedRouteNode;
            }
            set
            {
                this.Set(ref this.selectedRouteNode, value);
            }
        }

        /// <summary>Gets or sets the source.</summary>
        /// <exception cref="UnknownSystemException">Thrown if the system is not found in the database.</exception>
        public string Source
        {
            get
            {
                return this.source;
            }
            set
            {
                this.RoutePlanner.Source = value;
                if (this.Set(ref this.source, value))
                {
                    this.CalculateRoute();
                }
            }
        }

        /// <summary>Gets the swap systems command.</summary>
        public ICommand SwapSystemsCommand
        {
            get
            {
                return this.swapSystemsCommand ?? (this.swapSystemsCommand = new RelayCommand(
                                                                                 () =>
                                                                                     {
                                                                                         var temp = this.Source;
                                                                                         this.Source = this.Destination;
                                                                                         this.Destination = temp;
                                                                                     }));
            }
        }

        /// <summary>Gets or sets the timeout.</summary>
        public TimeSpan Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                if (this.Set(ref this.timeout, value))
                {
                    this.RoutePlanner.Timeout = this.Timeout;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether a
        ///     <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" /> was found on the
        ///     last calculation.
        /// </summary>
        public bool WasRouteFound
        {
            get
            {
                return this.wasRouteFound;
            }
            set
            {
                this.Set(ref this.wasRouteFound, value);
                if (!value)
                {
                    Log.Debug("Route was NOT found.");
                    this.CalculationTime = TimeSpan.MinValue;
                    this.NumberOfJumps = 0;
                    this.Distance = 0;
                }
                else
                {
                    Log.Debug("Route was found.");
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Avoids the specified
        ///     <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" /> node.
        /// </summary>
        /// <param name="routeNode">
        ///     The <see cref="HAST.Elite.Dangerous.DataAssistant.ViewModels.RoutePlannerViewModel.route" /> node.
        /// </param>
        public void Avoid(IRouteNode routeNode)
        {
            this.RoutePlanner.AvoidSystems.Add(routeNode.System);
            this.CalculateRoute();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Methods

        /// <summary>Calculates the route.</summary>
        internal void CalculateRoute()
        {
            if (this.Source == this.Destination)
            {
                return;
            }
            Log.DebugFormat(
                "CalculateRoute {0} to {1} ({2:F2}ly range).",
                this.Source,
                this.Destination,
                this.JumpRange);
            this.Route.Clear();
            try
            {
                this.WasRouteFound = this.RoutePlanner.Calculate();
                if (!this.WasRouteFound)
                {
                    Log.WarnFormat(
                        "No route found between {0} and {1} with jump range of {2:F2}.",
                        this.Source,
                        this.Destination,
                        this.JumpRange);
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Warn(e);
                this.WasRouteFound = false;
            }
            foreach (var node in this.RoutePlanner.Route)
            {
                this.Route.Add(node);
            }
            this.CalculationTime = this.RoutePlanner.CalculationTime;
            this.NumberOfJumps = this.Route.Count;
            this.Distance = this.Route.Sum(r => r.Distance);
            if (this.NumberOfJumps <= 0)
            {
                return;
            }
            if (Settings.Default.AutoCopyNextSystem)
            {
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        var system = this.Route.First().System;
                        Clipboard.SetDataObject(system.ToLowerInvariant(), false);
                        i = 10;
                        Log.DebugFormat("{0} copied to the clipboard!", system);
                    }
                    catch
                    {
                        Log.Warn("Unable to write to the clipboard, sleeping for 10ms!");
                        Thread.Sleep(10);
                    }
                }
            }
            this.SelectedRouteNode = this.Route.First();
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged
        ///     resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // free other managed objects that implement
                // IDisposable only
                this.RoutePlanner.Dispose();
            }

            // release any unmanaged objects
            // set the object references to null

            this.disposed = true;
        }

        #endregion
    }
}