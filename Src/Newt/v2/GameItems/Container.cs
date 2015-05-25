﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    #region Interface: IContainer

    /// <summary>
    /// This defines a class that is designed to hang on to quantities of stuff
    /// </summary>
    /// <remarks>
    /// You would use a container for all kinds of purposes (Ammo Clip, Money, Fuel Tank).  Basically anything that needs to be 
    /// packaged and transported around.
    /// 
    /// I was batting around the idea of allowing flow restrictions.  Think of a senario with an energy tank, a fuel tank, and an 
    /// energy-matter converter sitting between them.  But I decided that the restriction is in the converter, not the tanks.  The 
    /// biggest problem with having flow restrictions at the container level is how to enforce it.  If the max flow is ten, and there 
    /// were 100 requests for 10, then the container just let out 1000 fluid.  Did this occur over a second, or an hour?  Now every 
    /// container needs to get updates on the game's time.  While this is a technically accurate model, it is over complex, because
    /// it is most likely the user of my contents that has the real bottleneck (a machine gun has a very predictable rate of fire, etc.)
    /// </remarks>
    public interface IContainer
    {
        //TODO: This would be nice to have in the future
        //public event ValueMilestone(enum whichMileStone?, bool isUpStroke, double currentValue);

        double QuantityCurrent { get; set; }
        double QuantityMax { get; set; }
        double QuantityMaxMinusCurrent { get; }      // This one is for cases that need this value, and don't want to use two gets every time they need it calculated.

        /// <summary>
        /// This can be set up so that during a remove statement, you can only remove exact multiples of RemoveMultiple.  The
        /// most obvious choice for RemoveMultiple is the integer value of 1
        /// TODO: May want to constrain adds as well (do that with a separate property)
        /// </summary>
        bool OnlyRemoveMultiples { get; set; }
        /// <summary>
        /// NOTE: Only removes are enforced, partial adds are allowed.  This is for cases like ammo that is being slowly regenerated.  In reality, you can't make
        /// half a bullet, but it makes it much easier to implement this way (the bullets are stored here as simple numerical values, but you can't fire one until a
        /// complete bullet has been made.
        /// 
        /// If you really want adds enforced, make an AdditionMultiple property
        /// </summary>
        double RemovalMultiple { get; set; }

        /// <summary>
        /// This will try to add to the current quantity.  It will return the amount left over.  The only time the amount left over will
        /// be greater than zero is if the capacity is exceeded
        /// </summary>
        double AddQuantity(double amount, bool exactAmountOnly);

        /// <summary>
        /// This function will transfer stuff from the container passed in into this container.  returns the amount that COULDN'T be transfered
        /// </summary>
        /// <remarks>
        /// There are a couple situations that could happen.  It could be too full to hold all that was requested.  The source container may not have had
        /// enough to fulfill the request.
        /// </remarks>
        double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly);

        /// <summary>
        /// This will suck everything it possibly can from the container passed in, and add that to this class.
        /// The return variable will hold the amount that COULDN'T be transfered
        /// </summary>
        double AddQuantity(IContainer pullFrom, bool exactAmountOnly);

        /// <summary>
        /// This will try to deplete from this container.  It will return the amount left over from the request
        /// </summary>
        double RemoveQuantity(double amount, bool exactAmountOnly);
    }

    #endregion

    #region Class: Container_ThreadUnsafe

    public class Container_ThreadUnsafe : IContainer
    {
        #region Public Properties

        private double _quantityCurrent = 0d;
        public double QuantityCurrent
        {
            get
            {
                return _quantityCurrent;
            }
            set
            {
                if (value < 0d)
                {
                    if (Math3D.IsNearZero(value))
                    {
                        value = 0d;
                    }
                    else
                    {
                        throw new ArgumentException("Quantity can't be negative: " + value.ToString());
                    }
                }

                _quantityCurrent = value;

                // Cap it
                if (_quantityCurrent > _quantityMax)
                {
                    _quantityCurrent = _quantityMax;
                }
            }
        }

        private double _quantityMax = 1d;
        public double QuantityMax
        {
            get
            {
                return _quantityMax;
            }
            set
            {
                if (value < 0d)
                {
                    if (Math3D.IsNearZero(value))
                    {
                        value = 0d;
                    }
                    else
                    {
                        throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
                    }
                }

                _quantityMax = value;

                // Cap it
                if (_quantityCurrent > _quantityMax)
                {
                    _quantityCurrent = _quantityMax;
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                return _quantityMax - _quantityCurrent;
            }
        }

        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                return _onlyRemoveMultiples;
            }
            set
            {
                _onlyRemoveMultiples = value;
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                return _removalMultiple;
            }
            set
            {
                _removalMultiple = value;
            }
        }

        #endregion

        #region Public Methods

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            double actualAmount = amount;

            //   Figure out what should actually be stored
            if (_quantityCurrent + actualAmount > _quantityMax)
            {
                actualAmount = _quantityMax - _quantityCurrent;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            //   Add the value
            _quantityCurrent += actualAmount;

            //   Exit function
            return amount - actualAmount;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            double actualAmount = amount;

            // See if I can handle that much
            if (_quantityCurrent + actualAmount > _quantityMax)
            {
                actualAmount = _quantityMax - _quantityCurrent;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                // Now try to pull that much out of the source container
                actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                // Add the value
                _quantityCurrent += actualAmount;
            }

            // Exit function
            return amount - actualAmount;
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            // Run contraints on the requested amount to remove
            double actualAmount = GetRemoveAmount(this, amount, exactAmountOnly);

            // Remove the value
            _quantityCurrent -= actualAmount;

            // Exit function
            return amount - actualAmount;
        }

        /// <summary>
        /// This is a helper method to know what RemoveQuantity will take (between RemovalMultiple and exactAmountOnly, it
        /// gets a little complex)
        /// </summary>
        public static double GetRemoveAmount(IContainer container, double amount, bool exactAmountOnly)
        {
            double retVal = amount;

            // See if the outgoing flow needs to be restricted
            if (retVal > container.QuantityCurrent)
            {
                retVal = container.QuantityCurrent;
            }

            // See if it wants even multiples
            if (container.OnlyRemoveMultiples && !Math3D.IsDivisible(retVal, container.RemovalMultiple))
            {
                // Remove as many multiples of the requested amount as possible
                retVal = Math.Floor(retVal / container.RemovalMultiple) * container.RemovalMultiple;
            }

            // Exact amount
            if (exactAmountOnly && !Math3D.IsNearValue(retVal, amount))
            {
                retVal = 0d;
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ContainerGroup_ThreadUnsafe

    /// <summary>
    /// This represents a set of containers that share their contents evenly amongst themselves (instant transfer).  So from
    /// the outside, they can be treated like a single container.
    /// </summary>
    /// <remarks>
    /// A ship can have multiple energy tanks, and multiple fuel tanks.  But all the energy tanks will be treated like a single
    /// tank (same with all the fuel tanks).  The advantage of multple tanks vs a large single tank would be better mass
    /// distribution, and redundency.
    /// </remarks>
    public class ContainerGroup_ThreadUnsafe : IContainer
    {
        #region Enum: ContainerOwnershipType

        public enum ContainerOwnershipType
        {
            /// <summary>
            /// Neither quanities or maxes can change outside the knowledge of this group class (this allows for optimizations)
            /// </summary>
            GroupIsSoleOwner,
            /// <summary>
            /// Quantities can change outside this group class (but not maxes).  Whenever an add or remove is performed by this
            /// class, all the containers will be equalized first.
            /// </summary>
            QuantitiesCanChange,
            /// <summary>
            /// Both quanities and maxes could change outside this class.  Whenever an add or remove is performed by this class,
            /// the ratios will be recalculated, and the quantities will be equalized first.
            /// </summary>
            QuantitiesMaxesCanChange
        }

        #endregion

        #region Declaration Section

        private List<IContainer> _containers = new List<IContainer>();

        /// <summary>
        /// This is the ratio of each tank's max quantity compared to the sum of all the tanks
        /// </summary>
        private List<double> _ratios = new List<double>();

        // These are only used if ownership is full or partial (as an optimization)
        private double _current = 0d;
        private double _max = 0d;

        #endregion

        #region IContainer Members

        public double QuantityCurrent
        {
            get
            {
                if (_ownership == ContainerOwnershipType.GroupIsSoleOwner)
                {
                    return _current;
                }
                else
                {
                    //return _containers.Sum(o => o.QuantityCurrent);		// not using linq because this property will be called a lot, and I want it as fast as possible

                    double retVal = 0;
                    foreach (IContainer container in _containers)
                    {
                        retVal += container.QuantityCurrent;
                    }

                    return retVal;
                }
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentException("Quantity can't be negative: " + value.ToString());
                }

                double maxQuantity = this.QuantityMax;

                if (value > maxQuantity)
                {
                    foreach (IContainer container in _containers)
                    {
                        container.QuantityCurrent = container.QuantityMax;
                    }

                    _current = maxQuantity;		// no need to look at ownership, just set it
                }
                else
                {
                    if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
                    {
                        RecalcRatios();
                    }

                    // Distribute the values evenly (it doesn't matter what was there before)
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        _containers[cntr].QuantityCurrent = value * _ratios[cntr];
                    }

                    _current = value;
                }
            }
        }
        public double QuantityMax
        {
            get
            {
                if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
                {
                    //return _containers.Sum(o => o.QuantityMax);		// not using linq because this property will be called a lot, and I want it as fast as possible

                    double retVal = 0;
                    foreach (IContainer container in _containers)
                    {
                        retVal += container.QuantityMax;
                    }

                    return retVal;
                }
                else
                {
                    return _max;
                }
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
                }

                if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
                {
                    RecalcRatios();
                }

                // Distribute the values evenly (let each container handle what happens if there is now overflow)
                //NOTE: _ratios is based on each container's max relative to the sum of their maxes.  So the max will change, but the ratios will stay the same
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _containers[cntr].QuantityMax = value * _ratios[cntr];
                }

                _max = value;

                // Cap it
                if (this.QuantityCurrent > _max)
                {
                    this.QuantityCurrent = _max;
                }
            }
        }
        public double QuantityMaxMinusCurrent
        {
            get
            {
                return this.QuantityMax - this.QuantityCurrent;
            }
        }

        //NOTE: This class will still pull fractions across all containers, but will enforce even amounts from the global remove method.  This isn't technically correct, but is much easier :)
        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                return _onlyRemoveMultiples;
            }
            set
            {
                _onlyRemoveMultiples = value;
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                return _removalMultiple;
            }
            set
            {
                _removalMultiple = value;
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            double current = this.QuantityCurrent;
            double max = this.QuantityMax;

            double actualAmount = amount;

            //   Figure out what should actually be stored
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                #region Add it

                // Ensure that the containers are equalized
                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        // Nothing to do
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        EqualizeContainers(false);
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        EqualizeContainers(true);
                        break;

                    default:
                        throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }

                // Add the value evenly
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _containers[cntr].QuantityCurrent += actualAmount * _ratios[cntr];
                }

                // Cache the new value (this is used if sole owner)
                _current = current + actualAmount;

                #endregion
            }

            //   Exit function
            return amount - actualAmount;
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            double current = this.QuantityCurrent;
            double max = this.QuantityMax;

            double actualAmount = amount;

            // See if I can handle that much
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                // Now try to pull that much out of the source container
                actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                if (actualAmount != 0d)
                {
                    #region Add it

                    // Ensure that the containers are equalized
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // Nothing to do
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }

                    // Add the value evenly
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        _containers[cntr].QuantityCurrent += actualAmount * _ratios[cntr];
                    }

                    // Cache the new value (this is used if sole owner)
                    _current = current + actualAmount;

                    #endregion
                }
            }

            // Exit function
            return amount - actualAmount;
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            double current = this.QuantityCurrent;
            double max = this.QuantityMax;

            double actualAmount = amount;

            // See if I need to restrict the outgoing flow
            if (actualAmount > current)
            {
                actualAmount = current;
            }

            //NOTE: Only looking at the whole, not each individual container
            if (_onlyRemoveMultiples && !Math3D.IsDivisible(actualAmount, _removalMultiple))
            {
                // Remove as many multiples of the requested amount as possible
                actualAmount = Math.Floor(actualAmount / _removalMultiple) * _removalMultiple;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                #region Remove it

                // Ensure that the containers are equalized
                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        // Nothing to do
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        EqualizeContainers(false);
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        EqualizeContainers(true);
                        break;

                    default:
                        throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }

                // Remove the value evenly
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _containers[cntr].QuantityCurrent -= actualAmount * _ratios[cntr];
                }

                // Cache the new value (this is used if sole owner)
                _current = current - actualAmount;

                #endregion
            }

            // Exit function
            return amount - actualAmount;
        }

        #endregion

        #region Public Properties

        private ContainerOwnershipType _ownership = ContainerOwnershipType.QuantitiesMaxesCanChange;		// defaulting to the safest but most expensive setting
        public ContainerOwnershipType Ownership
        {
            get
            {
                return _ownership;
            }
            set
            {
                if (_ownership == value)
                {
                    return;
                }

                _ownership = value;

                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        EqualizeContainers(true);		// this should be the last time this needs to be called (unless containers are added/removed)
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        RecalcRatios();		// no need to call equalize now.  It will be called whenever any action against this group is requested
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        // no need to call anything right now
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Once this is added, the quantity is equalized across all containers
        /// </summary>
        public void AddContainer(IContainer container)
        {
            if (_containers.Contains(container))
            {
                throw new ArgumentException("The container was already added to this group");
            }

            _containers.Add(container);
            _ratios.Add(0d);

            // Cache the new values
            _current = _containers.Sum(o => o.QuantityCurrent);		// can't pull this.QuantityCurrent, because it uses _current
            _max = _containers.Sum(o => o.QuantityMax);

            EqualizeContainers(true);		// this needs to be done after setting _current
        }

        /// <summary>
        /// This will equalize the containers before removing it (if deplete is false)
        /// </summary>
        /// <remarks>
        /// If the container is destroyed/switched off, that would be a case for not depleting it first
        /// </remarks>
        public void RemoveContainer(IContainer container, bool depleteFirst)
        {
            int index = _containers.IndexOf(container);

            if (index < 0)
            {
                throw new ArgumentException("The container wasn't in the group");
            }

            if (!depleteFirst)
            {
                // Since they are removing the container, and don't want it emptied, make sure that it has an even share of the quantity
                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        // The containers are already equalized
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        EqualizeContainers(false);
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        EqualizeContainers(true);
                        break;

                    default:
                        throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }
            }

            // Pull it out - "sir, we are being probed" "send an away team"
            _containers.RemoveAt(index);
            _ratios.RemoveAt(index);

            // Cache the new values (must be done before calling other instance methods, since they may pull from these cached values)
            _current = _containers.Sum(o => o.QuantityCurrent);		// can't pull this.QuantityCurrent, because it uses _current
            _max = _containers.Sum(o => o.QuantityMax);

            EqualizeContainers(true);		// probably only need to call RecalcRatios, but this feels safer

            if (depleteFirst)
            {
                this.AddQuantity(container, false);		// ignoring this.OnlyRemoveMultiples, since partial adds are supported.  So the container will be fully depleted into the remaining containers, but any external call to get quantity will still have that enforced
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This compares the size of each container to the sum of all the container's sizes, and stores that percent in _ratios
        /// </summary>
        private void RecalcRatios()
        {
            double maxQuantity = this.QuantityMax;
            _ratios = _containers.Select(o => o.QuantityMax / maxQuantity).ToList();
        }

        /// <summary>
        /// This adds up all the quantity across the containers, and makes sure that each container holds the same percent of that quantity
        /// </summary>
        private void EqualizeContainers(bool recalcRatios)
        {
            if (recalcRatios)
            {
                RecalcRatios();
            }

            double current = this.QuantityCurrent;

            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                _containers[cntr].QuantityCurrent = current * _ratios[cntr];
            }
        }

        #endregion
    }

    #endregion

    #region Class: Container

    public class Container : IContainer
    {
        #region Declaration Section

        private readonly object _lock = new object();

        #endregion

        #region Public Properties

        private double _quantityCurrent = 0d;
        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _quantityCurrent;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value < 0d)
                    {
                        if (Math3D.IsNearZero(value))
                        {
                            value = 0d;
                        }
                        else
                        {
                            throw new ArgumentException("Quantity can't be negative: " + value.ToString());
                        }
                    }

                    _quantityCurrent = value;

                    // Cap it
                    if (_quantityCurrent > _quantityMax)
                    {
                        _quantityCurrent = _quantityMax;
                    }
                }
            }
        }

        private double _quantityMax = 1d;
        public double QuantityMax
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value < 0d)
                    {
                        if (Math3D.IsNearZero(value))
                        {
                            value = 0d;
                        }
                        else
                        {
                            throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
                        }
                    }

                    _quantityMax = value;

                    // Cap it
                    if (_quantityCurrent > _quantityMax)
                    {
                        _quantityCurrent = _quantityMax;
                    }
                }
            }
        }

        public double QuantityMaxMinusCurrent
        {
            get
            {
                lock (_lock)
                {
                    return _quantityMax - _quantityCurrent;
                }
            }
        }

        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                lock (_lock)
                {
                    return _onlyRemoveMultiples;
                }
            }
            set
            {
                lock (_lock)
                {
                    _onlyRemoveMultiples = value;
                }
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                lock (_lock)
                {
                    return _removalMultiple;
                }
            }
            set
            {
                lock (_lock)
                {
                    _removalMultiple = value;
                }
            }
        }

        #endregion

        #region Public Methods

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double actualAmount = amount;

                //   Figure out what should actually be stored
                if (_quantityCurrent + actualAmount > _quantityMax)
                {
                    actualAmount = _quantityMax - _quantityCurrent;
                }

                //if (exactAmountOnly && actualAmount != amount)
                if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                //   Add the value
                _quantityCurrent += actualAmount;

                //   Exit function
                return amount - actualAmount;
            }
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double actualAmount = amount;

                // See if I can handle that much
                if (_quantityCurrent + actualAmount > _quantityMax)
                {
                    actualAmount = _quantityMax - _quantityCurrent;
                }

                //if (exactAmountOnly && actualAmount != amount)
                if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                if (actualAmount != 0d)
                {
                    // Now try to pull that much out of the source container
                    actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                    // Add the value
                    _quantityCurrent += actualAmount;
                }

                // Exit function
                return amount - actualAmount;
            }
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            // Run contraints on the requested amount to remove
            double actualAmount = GetRemoveAmount(this, amount, exactAmountOnly);

            lock (_lock)
            {
                // Remove the value
                _quantityCurrent -= actualAmount;
            }

            // Exit function
            return amount - actualAmount;
        }

        /// <summary>
        /// This is a helper method to know what RemoveQuantity will take (between RemovalMultiple and exactAmountOnly, it
        /// gets a little complex)
        /// </summary>
        public static double GetRemoveAmount(IContainer container, double amount, bool exactAmountOnly)
        {
            //NOTE: I couldn't think of a way to make this totally threadsafe without adding more to the IContainer interface that returns all the needed
            //variables in one shot.  But in reality, OnlyRemoveMultiples and RemovalMultiple should change very infrequently if ever

            double retVal = amount;

            double quantityCurrent = container.QuantityCurrent;

            // See if the outgoing flow needs to be restricted
            if (retVal > quantityCurrent)
            {
                retVal = quantityCurrent;
            }

            // See if it wants even multiples
            if (container.OnlyRemoveMultiples)
            {
                double removalMultiple = container.RemovalMultiple;

                if (!Math3D.IsDivisible(retVal, removalMultiple))
                {
                    // Remove as many multiples of the requested amount as possible
                    retVal = Math.Floor(retVal / removalMultiple) * removalMultiple;
                }
            }

            // Exact amount
            if (exactAmountOnly && !Math3D.IsNearValue(retVal, amount))
            {
                retVal = 0d;
            }

            // Exit Function
            return retVal;
        }

        #endregion
    }

    #endregion
    #region Class: ContainerGroup

    //WARNING: When this group isn't the sole owner of the containers, then this isn't completely threadsafe - at least not atomic.  Some containers could
    //get more filled than others
    /// <summary>
    /// This represents a set of containers that share their contents evenly amongst themselves (instant transfer).  So from
    /// the outside, they can be treated like a single container.
    /// </summary>
    /// <remarks>
    /// A ship can have multiple energy tanks, and multiple fuel tanks.  But all the energy tanks will be treated like a single
    /// tank (same with all the fuel tanks).  The advantage of multple tanks vs a large single tank would be better mass
    /// distribution, and redundency.
    /// </remarks>
    public class ContainerGroup : IContainer
    {
        #region Enum: ContainerOwnershipType

        public enum ContainerOwnershipType
        {
            /// <summary>
            /// Neither quanities or maxes can change outside the knowledge of this group class (this allows for optimizations)
            /// </summary>
            GroupIsSoleOwner,
            /// <summary>
            /// Quantities can change outside this group class (but not maxes).  Whenever an add or remove is performed by this
            /// class, all the containers will be equalized first.
            /// </summary>
            QuantitiesCanChange,
            /// <summary>
            /// Both quanities and maxes could change outside this class.  Whenever an add or remove is performed by this class,
            /// the ratios will be recalculated, and the quantities will be equalized first.
            /// </summary>
            QuantitiesMaxesCanChange
        }

        #endregion

        #region Declaration Section

        private readonly object _lock = new object();

        private List<IContainer> _containers = new List<IContainer>();

        /// <summary>
        /// This is the ratio of each tank's max quantity compared to the sum of all the tanks
        /// </summary>
        private List<double> _ratios = new List<double>();

        // These are only used if ownership is full or partial (as an optimization)
        private double _current = 0d;
        private double _max = 0d;

        #endregion

        #region IContainer Members

        public double QuantityCurrent
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityCurrent();
                }
            }
            set
            {
                lock (_lock)
                {
                    SetQuantityCurrent(value);
                }
            }
        }
        public double QuantityMax
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax();
                }
            }
            set
            {
                lock (_lock)
                {
                    SetQuantityMax(value);
                }
            }
        }
        public double QuantityMaxMinusCurrent
        {
            get
            {
                lock (_lock)
                {
                    return GetQuantityMax() - GetQuantityCurrent();
                }
            }
        }

        //NOTE: This class will still pull fractions across all containers, but will enforce even amounts from the global remove method.  This isn't technically correct, but is much easier :)
        private bool _onlyRemoveMultiples = false;
        public bool OnlyRemoveMultiples
        {
            get
            {
                lock (_lock)
                {
                    return _onlyRemoveMultiples;
                }
            }
            set
            {
                lock (_lock)
                {
                    _onlyRemoveMultiples = value;
                }
            }
        }

        private double _removalMultiple = 1d;
        public double RemovalMultiple
        {
            get
            {
                lock (_lock)
                {
                    return _removalMultiple;
                }
            }
            set
            {
                lock (_lock)
                {
                    _removalMultiple = value;
                }
            }
        }

        public double AddQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                return AddQuantity_priv(amount, exactAmountOnly);
            }
        }
        public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity_priv(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                return AddQuantity_priv(pullFrom, amount, exactAmountOnly);
            }
        }

        public double RemoveQuantity(double amount, bool exactAmountOnly)
        {
            lock (_lock)
            {
                double current = GetQuantityCurrent();
                double max = GetQuantityMax();

                double actualAmount = amount;

                // See if I need to restrict the outgoing flow
                if (actualAmount > current)
                {
                    actualAmount = current;
                }

                //NOTE: Only looking at the whole, not each individual container
                if (_onlyRemoveMultiples && !Math3D.IsDivisible(actualAmount, _removalMultiple))
                {
                    // Remove as many multiples of the requested amount as possible
                    actualAmount = Math.Floor(actualAmount / _removalMultiple) * _removalMultiple;
                }

                //if (exactAmountOnly && actualAmount != amount)
                if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
                {
                    actualAmount = 0d;
                }

                if (actualAmount != 0d)
                {
                    #region Remove it

                    // Ensure that the containers are equalized
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // Nothing to do
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }

                    // Remove the value evenly
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        _containers[cntr].QuantityCurrent -= actualAmount * _ratios[cntr];
                    }

                    // Cache the new value (this is used if sole owner)
                    _current = current - actualAmount;

                    #endregion
                }

                // Exit function
                return amount - actualAmount;
            }
        }

        #endregion

        #region Public Properties

        private ContainerOwnershipType _ownership = ContainerOwnershipType.QuantitiesMaxesCanChange;		// defaulting to the safest but most expensive setting
        public ContainerOwnershipType Ownership
        {
            get
            {
                lock (_lock)
                {
                    return _ownership;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_ownership == value)
                    {
                        return;
                    }

                    _ownership = value;

                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            EqualizeContainers(true);		// this should be the last time this needs to be called (unless containers are added/removed)
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            RecalcRatios();		// no need to call equalize now.  It will be called whenever any action against this group is requested
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            // no need to call anything right now
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Once this is added, the quantity is equalized across all containers
        /// </summary>
        public void AddContainer(IContainer container)
        {
            lock (_lock)
            {
                if (_containers.Contains(container))
                {
                    throw new ArgumentException("The container was already added to this group");
                }

                _containers.Add(container);
                _ratios.Add(0d);

                // Cache the new values
                _current = _containers.Sum(o => o.QuantityCurrent);		// can't pull this.QuantityCurrent, because it uses _current
                _max = _containers.Sum(o => o.QuantityMax);

                EqualizeContainers(true);		// this needs to be done after setting _current
            }
        }

        /// <summary>
        /// This will equalize the containers before removing it (if deplete is false)
        /// </summary>
        /// <remarks>
        /// If the container is destroyed/switched off, that would be a case for not depleting it first
        /// </remarks>
        public void RemoveContainer(IContainer container, bool depleteFirst)
        {
            lock (_lock)
            {
                int index = _containers.IndexOf(container);

                if (index < 0)
                {
                    throw new ArgumentException("The container wasn't in the group");
                }

                if (!depleteFirst)
                {
                    // Since they are removing the container, and don't want it emptied, make sure that it has an even share of the quantity
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // The containers are already equalized
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }
                }

                // Pull it out - "sir, we are being probed" "send an away team"
                _containers.RemoveAt(index);
                _ratios.RemoveAt(index);

                // Cache the new values (must be done before calling other instance methods, since they may pull from these cached values)
                _current = _containers.Sum(o => o.QuantityCurrent);		// can't pull this.QuantityCurrent, because it uses _current
                _max = _containers.Sum(o => o.QuantityMax);

                EqualizeContainers(true);		// probably only need to call RecalcRatios, but this feels safer

                if (depleteFirst)
                {
                    AddQuantity_priv(container, false);		// ignoring this.OnlyRemoveMultiples, since partial adds are supported.  So the container will be fully depleted into the remaining containers, but any external call to get quantity will still have that enforced
                }
            }
        }

        #endregion

        #region Private Methods

        // ---------------- These are private implementations of the publics.  Made them private methods so they can be called from within a lock
        private double GetQuantityCurrent()
        {
            if (_ownership == ContainerOwnershipType.GroupIsSoleOwner)
            {
                return _current;
            }
            else
            {
                //return _containers.Sum(o => o.QuantityCurrent);		// not using linq because this property will be called a lot, and I want it as fast as possible

                double retVal = 0;
                foreach (IContainer container in _containers)
                {
                    retVal += container.QuantityCurrent;
                }

                return retVal;
            }
        }
        private void SetQuantityCurrent(double value)
        {
            if (value < 0d)
            {
                throw new ArgumentException("Quantity can't be negative: " + value.ToString());
            }

            double maxQuantity = GetQuantityMax();

            if (value > maxQuantity)
            {
                foreach (IContainer container in _containers)
                {
                    container.QuantityCurrent = container.QuantityMax;
                }

                _current = maxQuantity;		// no need to look at ownership, just set it
            }
            else
            {
                if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
                {
                    RecalcRatios();
                }

                // Distribute the values evenly (it doesn't matter what was there before)
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _containers[cntr].QuantityCurrent = value * _ratios[cntr];
                }

                _current = value;
            }
        }

        private double GetQuantityMax()
        {
            if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
            {
                //return _containers.Sum(o => o.QuantityMax);		// not using linq because this property will be called a lot, and I want it as fast as possible

                double retVal = 0;
                foreach (IContainer container in _containers)
                {
                    retVal += container.QuantityMax;
                }

                return retVal;
            }
            else
            {
                return _max;
            }
        }
        private void SetQuantityMax(double value)
        {
            if (value < 0d)
            {
                throw new ArgumentException("Max Quantity can't be negative: " + value.ToString());
            }

            if (_ownership == ContainerOwnershipType.QuantitiesMaxesCanChange)
            {
                RecalcRatios();
            }

            // Distribute the values evenly (let each container handle what happens if there is now overflow)
            //NOTE: _ratios is based on each container's max relative to the sum of their maxes.  So the max will change, but the ratios will stay the same
            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                _containers[cntr].QuantityMax = value * _ratios[cntr];
            }

            _max = value;

            // Cap it
            if (GetQuantityCurrent() > _max)
            {
                SetQuantityCurrent(_max);
            }
        }

        private double AddQuantity_priv(double amount, bool exactAmountOnly)
        {
            double current = GetQuantityCurrent();
            double max = GetQuantityMax();

            double actualAmount = amount;

            //   Figure out what should actually be stored
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                #region Add it

                // Ensure that the containers are equalized
                switch (_ownership)
                {
                    case ContainerOwnershipType.GroupIsSoleOwner:
                        // Nothing to do
                        break;

                    case ContainerOwnershipType.QuantitiesCanChange:
                        EqualizeContainers(false);
                        break;

                    case ContainerOwnershipType.QuantitiesMaxesCanChange:
                        EqualizeContainers(true);
                        break;

                    default:
                        throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                }

                // Add the value evenly
                for (int cntr = 0; cntr < _containers.Count; cntr++)
                {
                    _containers[cntr].QuantityCurrent += actualAmount * _ratios[cntr];
                }

                // Cache the new value (this is used if sole owner)
                _current = current + actualAmount;

                #endregion
            }

            //   Exit function
            return amount - actualAmount;
        }
        private double AddQuantity_priv(IContainer pullFrom, bool exactAmountOnly)
        {
            return AddQuantity_priv(pullFrom, pullFrom.QuantityCurrent, exactAmountOnly);
        }
        private double AddQuantity_priv(IContainer pullFrom, double amount, bool exactAmountOnly)
        {
            double current = GetQuantityCurrent();
            double max = GetQuantityMax();

            double actualAmount = amount;

            // See if I can handle that much
            if (current + actualAmount > max)
            {
                actualAmount = max - current;
            }

            //if (exactAmountOnly && actualAmount != amount)
            if (exactAmountOnly && !Math3D.IsNearValue(actualAmount, amount))
            {
                actualAmount = 0d;
            }

            if (actualAmount != 0d)
            {
                // Now try to pull that much out of the source container
                actualAmount -= pullFrom.RemoveQuantity(actualAmount, exactAmountOnly);

                if (actualAmount != 0d)
                {
                    #region Add it

                    // Ensure that the containers are equalized
                    switch (_ownership)
                    {
                        case ContainerOwnershipType.GroupIsSoleOwner:
                            // Nothing to do
                            break;

                        case ContainerOwnershipType.QuantitiesCanChange:
                            EqualizeContainers(false);
                            break;

                        case ContainerOwnershipType.QuantitiesMaxesCanChange:
                            EqualizeContainers(true);
                            break;

                        default:
                            throw new ApplicationException("Unknown ContainerOwnershipType: " + _ownership.ToString());
                    }

                    // Add the value evenly
                    for (int cntr = 0; cntr < _containers.Count; cntr++)
                    {
                        _containers[cntr].QuantityCurrent += actualAmount * _ratios[cntr];
                    }

                    // Cache the new value (this is used if sole owner)
                    _current = current + actualAmount;

                    #endregion
                }
            }

            // Exit function
            return amount - actualAmount;
        }
        // ------------------------------------------------

        /// <summary>
        /// This compares the size of each container to the sum of all the container's sizes, and stores that percent in _ratios
        /// </summary>
        private void RecalcRatios()
        {
            double maxQuantity = GetQuantityMax();

            if (Math3D.IsNearZero(maxQuantity))
            {
                _ratios = Enumerable.Range(0, _containers.Count).Select(o => 0d).ToList();
            }
            else
            {
                _ratios = _containers.Select(o => o.QuantityMax / maxQuantity).ToList();
            }
        }

        /// <summary>
        /// This adds up all the quantity across the containers, and makes sure that each container holds the same percent of that quantity
        /// </summary>
        private void EqualizeContainers(bool recalcRatios)
        {
            if (recalcRatios)
            {
                RecalcRatios();
            }

            double current = GetQuantityCurrent();

            for (int cntr = 0; cntr < _containers.Count; cntr++)
            {
                _containers[cntr].QuantityCurrent = current * _ratios[cntr];
            }
        }

        #endregion
    }

    #endregion
}