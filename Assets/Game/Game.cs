using System;
using UnityEngine;

namespace Framework.CMFR
{
    public class Game : MonoBehaviour,IController
    {

        // public RenderTexture rt;
        private void Awake()
        {
            this.RegisterEvent<GameStartEvent>(OnGameStart);
            

        }
        
        private void OnGameStart(GameStartEvent e)
        {

        }

        // private void OnRenderImage(RenderTexture source, RenderTexture destination)
        // {
        //     if (rt != null)
        //     {
        //         Graphics.Blit( rt , destination );
        //     }
        // }

        private void OnDestroy()
        {
            this.UnRegisterEvent<GameStartEvent>(OnGameStart);
        }

        public IArchitecture GetArchitecture()
        {
            return CMFRDemo.Interface;
        }
    }
}