using System;
using Framework.CMFR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Framework.CMFR
{
    public class ViewController : MonoBehaviour
    {
        private ICMFRModel mCMFRModel;
        public GameObject SliderPanel;
        public GameObject DropDownPanel;
        public Transform ScrollViewContentTransform;

        private void Start()
        {
            GameObject game = GameObject.Find("Game");
            Game gameComp = game.GetComponent<Game>() ;
            mCMFRModel = gameComp?.GetArchitecture().GetModel<ICMFRModel>();
            InitUI();
            
        }

        private void InitUI()
        {
            GameObject panel1 = Instantiate(SliderPanel);
            panel1.transform.SetParent(ScrollViewContentTransform);
            
            GameObject gameObject_Strategy = Instantiate(DropDownPanel);
            gameObject_Strategy.transform.SetParent(ScrollViewContentTransform);
            Dropdown dropdown_Strategy = gameObject_Strategy.transform.GetChild(2).GetComponent<Dropdown>();
            dropdown_Strategy.options.Clear();
            string[] strategyNames = Enum.GetNames(typeof(MappingStrategy));
            foreach (var name in strategyNames)
            {
                dropdown_Strategy.options.Add( new Dropdown.OptionData( name ));
            }
            
        }
    }
}