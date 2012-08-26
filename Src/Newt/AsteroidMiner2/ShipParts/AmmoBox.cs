﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Game.Newt.AsteroidMiner2.ShipEditor;
using System.Windows.Media;

using Game.Newt.HelperClasses;
using Game.Newt.NewtonDynamics;

namespace Game.Newt.AsteroidMiner2.ShipParts
{
	#region Class: AmmoBoxToolItem

	public class AmmoBoxToolItem : PartToolItemBase
	{
		#region Constructor

		public AmmoBoxToolItem(EditorOptions options)
			: base(options)
		{
			_visual2D = PartToolItemBase.GetVisual2D(this.Name, this.Description, options.EditorColors);
			this.TabName = PartToolItemBase.TAB_SHIPPART;
		}

		#endregion

		#region Public Properties

		public override string Name
		{
			get
			{
				return "Ammo Box";
			}
		}
		public override string Description
		{
			get
			{
				return "Stores ammo for projectile weapons";
			}
		}
		public override string Category
		{
			get
			{
				return PartToolItemBase.CATEGORY_CONTAINER;
			}
		}

		private UIElement _visual2D = null;
		public override UIElement Visual2D
		{
			get
			{
				return _visual2D;
			}
		}

		#endregion

		#region Public Methods

		public override PartDesignBase GetNewDesignPart()
		{
			return new AmmoBoxDesign(this.Options);
		}

		#endregion
	}

	#endregion
	#region Class: AmmoBoxDesign

	public class AmmoBoxDesign : PartDesignBase
	{
		#region Declaration Section

		public const double RATIOY = .6d;
		public const double RATIOZ = .7d;

		#endregion

		#region Constructor

		public AmmoBoxDesign(EditorOptions options)
			: base(options) { }

		#endregion

		#region Public Properties

		public override PartDesignAllowedScale AllowedScale
		{
			get
			{
				return PartDesignAllowedScale.X_Y_Z;
			}
		}
		public override PartDesignAllowedRotation AllowedRotation
		{
			get
			{
				return PartDesignAllowedRotation.X_Y_Z;
			}
		}

		private Model3DGroup _geometries = null;
		public override Model3D Model
		{
			get
			{
				if (_geometries == null)
				{
					_geometries = CreateGeometry(false);
				}

				return _geometries;
			}
		}

		#endregion

		#region Public Methods

		public override Model3D GetFinalModel()
		{
			return CreateGeometry(true);
		}

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			Transform3DGroup transform = new Transform3DGroup();
			//transform.Children.Add(new ScaleTransform3D(this.Scale));		//	it ignores scale
			transform.Children.Add(new RotateTransform3D(new QuaternionRotation3D(this.Orientation)));
			transform.Children.Add(new TranslateTransform3D(this.Position.ToVector()));

			Vector3D scale = this.Scale;
			Vector3D size = new Vector3D(1d * scale.X, RATIOY * scale.Y, RATIOZ * scale.Z);

			return CollisionHull.CreateBox(world, 0, size, transform.Value);
		}

		#endregion

		#region Private Methods

		private Model3DGroup CreateGeometry(bool isFinal)
		{
			const double PLATEHEIGHT = .1d;
			const double PLATEHEIGHTHALF = PLATEHEIGHT * .5d;
			const double PLATEHEIGHTQUARTER = PLATEHEIGHT * .25d;

			Model3DGroup retVal = new Model3DGroup();

			GeometryModel3D geometry;
			MaterialGroup material;
			DiffuseMaterial diffuse;
			SpecularMaterial specular;

			#region Main Box

			geometry = new GeometryModel3D();
			material = new MaterialGroup();
			diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.AmmoBox));
			this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.AmmoBox));
			material.Children.Add(diffuse);
			specular = this.Options.WorldColors.AmmoBoxSpecular;
			this.MaterialBrushes.Add(new MaterialColorProps(specular));
			material.Children.Add(specular);

			if (!isFinal)
			{
				EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
				material.Children.Add(selectionEmissive);
				this.SelectionEmissives.Add(selectionEmissive);
			}

			geometry.Material = material;
			geometry.BackMaterial = material;

			geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-.5d + PLATEHEIGHTQUARTER, (-.5d + PLATEHEIGHTQUARTER) * RATIOY, -.5d * RATIOZ), new Point3D(.5d - PLATEHEIGHTQUARTER, (.5d - PLATEHEIGHTQUARTER) * RATIOY, .5d * RATIOZ));

			retVal.Children.Add(geometry);

			#endregion

			#region Plates

			for (int cntr = -1; cntr <= 1; cntr++)
			{
				geometry = new GeometryModel3D();
				material = new MaterialGroup();
				diffuse = new DiffuseMaterial(new SolidColorBrush(this.Options.WorldColors.AmmoBoxPlate));
				this.MaterialBrushes.Add(new MaterialColorProps(diffuse, diffuse.Brush, this.Options.WorldColors.AmmoBoxPlate));
				material.Children.Add(diffuse);
				specular = this.Options.WorldColors.AmmoBoxPlateSpecular;
				this.MaterialBrushes.Add(new MaterialColorProps(specular));
				material.Children.Add(specular);

				if (!isFinal)
				{
					EmissiveMaterial selectionEmissive = new EmissiveMaterial(Brushes.Transparent);
					material.Children.Add(selectionEmissive);
					this.SelectionEmissives.Add(selectionEmissive);
				}

				geometry.Material = material;
				geometry.BackMaterial = material;

				double z = (.5d - PLATEHEIGHT + PLATEHEIGHTQUARTER) * cntr;
				geometry.Geometry = UtilityWPF.GetCube_IndependentFaces(new Point3D(-.5d, -.5d * RATIOY, (z - PLATEHEIGHTHALF) * RATIOZ), new Point3D(.5d, .5d * RATIOY, (z + PLATEHEIGHTHALF) * RATIOZ));

				retVal.Children.Add(geometry);
			}

			#endregion

			//	Transform
			Transform3DGroup transformGlobal = new Transform3DGroup();
			if (isFinal)
			{
				transformGlobal.Children.Add(_scaleTransform.Clone());
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform.Clone()));
				transformGlobal.Children.Add(_translateTransform.Clone());
			}
			else
			{
				transformGlobal.Children.Add(_scaleTransform);
				transformGlobal.Children.Add(new RotateTransform3D(_rotateTransform));
				transformGlobal.Children.Add(_translateTransform);
			}
			retVal.Transform = transformGlobal;

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
	#region Class: AmmoBox

	public class AmmoBox : PartBase, IContainer
	{
		#region Declaration Section

		public const string PARTTYPE = "AmmoBox";

		private AmmoBoxDesign _design = null;

		private Container _container = null;

		private ItemOptions _itemOptions = null;

		#endregion

		#region Constructor

		public AmmoBox(EditorOptions options, ItemOptions itemOptions, PartDNA dna)
			: base(options, dna)
		{
			_itemOptions = itemOptions;

			_design = new AmmoBoxDesign(options);
			_design.SetDNA(dna);

			double surfaceArea;
			_container = GetContainer(out surfaceArea, itemOptions, dna);

			_dryMass = surfaceArea * itemOptions.AmmoBoxWallDensity;
		}

		#endregion

		#region IContainer Members

		public double QuantityCurrent
		{
			get
			{
				return _container.QuantityCurrent;
			}
			set
			{
				_container.QuantityCurrent = value;
				base.OnMassChanged();
			}
		}
		public double QuantityMax
		{
			get
			{
				return _container.QuantityMax;
			}
			set
			{
				//_container.QuantityMax = value;
				throw new NotSupportedException("The container max can't be set directly.  It is derived from the dna.scale");
			}
		}
		public double QuantityMaxMinusCurrent
		{
			get
			{
				return _container.QuantityMaxMinusCurrent;
			}
		}

		//NOTE: _container.OnlyRemoveMultiples is already true
		public bool OnlyRemoveMultiples
		{
			get
			{
				return true;
			}
			set
			{
				if (!value)
				{
					throw new ArgumentException("The ammo box only supports true");
				}
			}
		}

		private double? _ammoSize = null;
		/// <summary>
		/// This is what size of ammo this box will hold
		/// </summary>
		/// <remarks>
		/// This isn't part of the dna, because dna.scale represents the volume of all the ammo that can be carried.  But ammo size is based
		/// on the size of gun that is tied to this ammo box.
		/// 
		/// For example, this can hold cannon balls with a size of 1.  Then later be hooked to a gun that fires cannon balls with a size of 5.
		/// 
		/// Note that this class assumes there is no dead space between ammo.  If the ammo is spheres, there would be tetrahedron shaped
		/// voids between the balls, and you'd be able to hold more ammo if it's smaller.  But I don't see much need to go that far
		/// 
		/// If you really want a justification for cube shaped ammo, think of them as cylindrical slugs, and the corners are shaved off and
		/// used as propellent.  It could even be shape changing material that takes a cone shape once fired :)
		/// </remarks>
		public double RemovalMultiple
		{
			get
			{
				return _ammoSize == null ? -1d : _ammoSize.Value;
			}
			set
			{
				if (_ammoSize != null && _ammoSize.Value == value)
				{
					//	No change
					return;
				}

				_ammoSize = value;

				_container.RemovalMultiple = _ammoSize.Value;

				//	Setting the quantity to zero, because whatever ammo was in here is now the wrong size
				if (_container.QuantityCurrent > 0d)
				{
					_container.QuantityCurrent = 0d;
					base.OnMassChanged();
				}
			}
		}

		public double AddQuantity(double amount, bool exactAmountOnly)
		{
			double retVal = _container.AddQuantity(amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}
		public double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly)
		{
			double retVal = _container.AddQuantity(pullFrom, amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}
		public double AddQuantity(IContainer pullFrom, bool exactAmountOnly)
		{
			double prevQuantity = this.QuantityCurrent;

			double retVal = _container.AddQuantity(pullFrom, exactAmountOnly);

			if (this.QuantityCurrent > prevQuantity)
			{
				OnMassChanged();
			}

			return retVal;
		}

		public double RemoveQuantity(double amount, bool exactAmountOnly)
		{
			double retVal = _container.RemoveQuantity(amount, exactAmountOnly);

			if (retVal < amount)
			{
				OnMassChanged();
			}

			return retVal;
		}

		#endregion

		#region Public Properties

		private double _dryMass = 0d;
		public override double DryMass
		{
			get
			{
				return _dryMass;
			}
		}
		public override double TotalMass
		{
			get
			{
				return this.DryMass + (_container.QuantityCurrent * _itemOptions.AmmoDensity);
			}
		}

		public override Point3D Position
		{
			get
			{
				return this.DNA.Position;
			}
		}
		public override Quaternion Orientation
		{
			get
			{
				return this.DNA.Orientation;
			}
		}

		private Model3D _model = null;
		public override Model3D Model
		{
			get
			{
				if (_model == null)
				{
					_model = _design.GetFinalModel();
				}

				return _model;
			}
		}

		#endregion

		#region Public Methods

		public override CollisionHull CreateCollisionHull(WorldBase world)
		{
			return _design.CreateCollisionHull(world);
		}

		#endregion

		#region Private Methods

		private static Container GetContainer(out double surfaceArea, ItemOptions itemOptions, PartDNA dna)
		{
			Container retVal = new Container();
			retVal.OnlyRemoveMultiples = true;

			double x = dna.Scale.X;
			double y = dna.Scale.Y * AmmoBoxDesign.RATIOY;
			double z = dna.Scale.Z * AmmoBoxDesign.RATIOZ;

			//	Volume
			retVal.QuantityMax = x * y * z;

			//	Surface Area
			surfaceArea = (2 * x * y) + (2 * x * z) + (2 * y * z);

			//	Exit Function
			return retVal;
		}

		#endregion
	}

	#endregion
}