using System;
using Command;
using Framework.CMFR;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace Framework.CMFR
{
    public class ViewController : MonoBehaviour , IController
    {
        private ICMFRModel mCMFRModel;
        public GameObject SliderPanel;
        public GameObject DropDownPanel;
        public GameObject TogglePanel;
        public Transform ScrollViewContentTransform;
        public Button btnSwitch;
        public GameObject PanelGM;

        private void Start()
        {
            GameObject game = GameObject.Find("Game");
            Game gameComp = game.GetComponent<Game>() ;
            mCMFRModel = gameComp?.GetArchitecture().GetModel<ICMFRModel>();
            InitUI();
            
            
        }

        private void InitUI()
        {
            // button switch
            btnSwitch.onClick.AddListener( () =>
            {
                mCMFRModel.GM_On.Value = !mCMFRModel.GM_On.Value;
                PanelGM.SetActive( mCMFRModel.GM_On.Value );
            });

            
            // output mode
            GameObject panelOutput = Instantiate(DropDownPanel);
            SetNameForPanel( panelOutput , "输出"  );
            SetUpDropDownForPanel( panelOutput , typeof(OutputTex) , mCMFRModel.outputTex );

            // Mapping Strategy
            GameObject panelStrat = Instantiate(DropDownPanel);
            SetNameForPanel( panelStrat , "映射策略");
            SetUpDropDownForPanel( panelStrat , typeof(MappingStrategy ) , mCMFRModel.mappingStrategy );
            

            // sigma
            GameObject panelSigma = Instantiate(SliderPanel);
            SetNameForPanel( panelSigma , "sigma" );
            SetUpSliderForPanel( panelSigma , mCMFRModel.sigma , 1.0f , 3.0f );

            // fx
            GameObject panelFx = Instantiate(SliderPanel);
            SetNameForPanel( panelFx , "fx" );
            SetUpSliderForPanel( panelFx , mCMFRModel.fx , 0.01f , 0.99f );
            
            // fy
            GameObject panelFy = Instantiate(SliderPanel);
            SetNameForPanel( panelFy , "fy" );
            SetUpSliderForPanel( panelFy , mCMFRModel.fy , 0.01f , 0.99f );
            
            // taa
            GameObject panelTaa = Instantiate(TogglePanel);
            SetNameForPanel( panelTaa , "TAA");
            SetUpToggleForPanel( panelTaa , mCMFRModel.TAA_On );    
            
            // jitter
            GameObject panelJitter = Instantiate(TogglePanel);
            SetNameForPanel( panelJitter , "Frustum Jitter");
            SetUpToggleForPanel( panelJitter , mCMFRModel.FrustumJitter_On );
        }

        private void SetNameForPanel(GameObject panel , string name )
        {
            Text textComp  = panel.transform.GetChild(0).GetComponent<Text>();
            if(textComp) textComp.text = name;
        }

        private T GetCompForPanel<T>(GameObject panel ) where T : Component
        {
            return panel.transform.GetChild(2).GetComponent<T>();
            
        }

        private void SetUpDropDownForPanel(GameObject panel, Type enumType, BindableProperty<int> property)
        {
            panel.transform.SetParent(ScrollViewContentTransform);
            Dropdown dropdown = GetCompForPanel<Dropdown>(panel);
            dropdown.options.Clear();
            SetOptionsForDropDown( dropdown , enumType );
            dropdown.onValueChanged.AddListener(value =>
            {
                property.Value = value;
            });
            dropdown.value = property.Value;
        }

        private void SetUpSliderForPanel(GameObject panel, BindableProperty<float> property , float minValue , float maxValue )
        {
            panel.transform.SetParent(ScrollViewContentTransform);
            Slider slider = GetCompForPanel<Slider>(panel);
            Text valueText = panel.transform.GetChild(1).GetComponent<Text>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = property.Value;
            valueText.text = property.Value.ToString();
            slider.onValueChanged.AddListener(value =>
            {
                property.Value = value;
                value = (float) Math.Round(value, 3); 
                valueText.text = value.ToString() ; 
                if( property == mCMFRModel.sigma ) this.SendCommand<GBufferSizeChangeCommand>();
            });
        }

        void SetUpToggleForPanel(GameObject panel, BindableProperty<bool> property)
        {
            panel.transform.SetParent(ScrollViewContentTransform);
            Toggle toggle = GetCompForPanel<Toggle>(panel);
            toggle.onValueChanged.AddListener(value =>
            {
                property.Value = value;
            });
            toggle.isOn = property.Value;
        }
        
        private void SetOptionsForDropDown(Dropdown comp, Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            foreach (var name in names)
            {
                comp.options.Add( new Dropdown.OptionData( name ));
            }
        }
        
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return CMFRDemo.Interface;
        }
    }
}