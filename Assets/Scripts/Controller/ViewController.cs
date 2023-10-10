using System;
using Framework.CMFR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Framework.CMFR
{
    public class ViewController : MonoBehaviour
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
            #region Output Texture 
            
            GameObject panelOutput = Instantiate(DropDownPanel);
            panelOutput.transform.SetParent(ScrollViewContentTransform);
            SetNameForPanel( panelOutput , "输出"  );
            SetUpDropDownForPanel( panelOutput , typeof(OutputTex) , mCMFRModel.outputTex );
            
            #endregion
            
            
            
            #region Mapping Strategy
            
            GameObject panelStrat = Instantiate(DropDownPanel);
            panelStrat.transform.SetParent(ScrollViewContentTransform);
            SetNameForPanel( panelStrat , "映射策略");
            SetUpDropDownForPanel( panelStrat , typeof(MappingStrategy ) , mCMFRModel.mappingStrategy );

            
            #endregion


            #region Btn Switch

            btnSwitch.onClick.AddListener( () =>
            {
                mCMFRModel.GM_On.Value = !mCMFRModel.GM_On.Value;
                PanelGM.SetActive( mCMFRModel.GM_On.Value );
            });

            #endregion
            
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
            Dropdown dropdown = GetCompForPanel<Dropdown>(panel);
            dropdown.options.Clear();
            SetOptionsForDropDown( dropdown , enumType );
            dropdown.onValueChanged.AddListener(value =>
            {
                property.Value = value;
            });
            dropdown.value = property.Value;
        }
        
        private void SetOptionsForDropDown(Dropdown comp, Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            foreach (var name in names)
            {
                comp.options.Add( new Dropdown.OptionData( name ));
            }
        }
    }
}