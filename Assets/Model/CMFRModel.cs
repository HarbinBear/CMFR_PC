namespace Framework.CMFR
{
    
    public enum MappingStrategy
    {
        RMFR                        = 0 ,    
    
        Elliptical_Grid_Mapping     = 1 ,
        Squelched_Grid_Mapping      = 2 ,
        Blended_E_Grid_Mapping      = 3 ,
                                  
        FG_Squircular_Mapping       = 4 ,
        Two_Squircular_Mapping      = 5 ,
        Sham_Quartic_Mapping        = 6 ,
    
        Schwarz_Christoffel_Mapping = 7 ,
    
        Hyperbolic_Mapping          = 8 ,
        cornerific_tapered2         = 9 ,
        Non_axial_2_pinch           = 10,
    
        Simple_Strech               = 11,
    }
    
    public interface ICMFRModel : IModel
    {
        BindableProperty<float> sigma { get;  }
        BindableProperty<float> fx { get;  }
        BindableProperty<float> fy { get;  }
        BindableProperty<float> eyeX { get;  }
        BindableProperty<float> eyeY { get;  }
        BindableProperty<float> scaleRatio { get;  }
        BindableProperty<float> sampleDensityWithNoise { get;  }
        BindableProperty<float> squelchedGridMappingBeta { get;  }
        BindableProperty<MappingStrategy> mappingStrategy { get;  }
        BindableProperty<int> iApplyRFRMap1 { get;  }
        BindableProperty<int> debugMode { get;  }

        
        
    }


    public class CMFRModel : AbstractModel, ICMFRModel
    {
        
        public BindableProperty<float> sigma { get; } = new BindableProperty<float>()
        {
            Value = 2.2f
        };
        public BindableProperty<float> fx { get; } = new BindableProperty<float>()
        {
            Value = 0.2f
        };
        public BindableProperty<float> fy { get; } = new BindableProperty<float>()
        {
            Value = 0.2f
        };
        public BindableProperty<float> eyeX { get; } = new BindableProperty<float>()
        {
            Value = 0.5f
        };
        public BindableProperty<float> eyeY { get; } = new BindableProperty<float>()
        {
            Value = 0.5f
        };
        public BindableProperty<float> squelchedGridMappingBeta { get; } = new BindableProperty<float>()
        {
            Value = 0.052f
        };
        public BindableProperty<float> sampleDensityWithNoise { get; } = new BindableProperty<float>()
        {
            Value = 1
        };
        public BindableProperty<float> scaleRatio { get; } = new BindableProperty<float>()
        {
            Value = 1
        };
        public BindableProperty<int> iApplyRFRMap1 { get; } = new BindableProperty<int>()
        {
            Value = 1
        };
        public BindableProperty<int> debugMode { get; } = new BindableProperty<int>()
        {
            Value = 0
        };
        public BindableProperty<MappingStrategy> mappingStrategy { get; } = new BindableProperty<MappingStrategy>()
        {
            Value = MappingStrategy.Squelched_Grid_Mapping
        };
        
        protected override void OnInit()
        {

        
        }
    }
}