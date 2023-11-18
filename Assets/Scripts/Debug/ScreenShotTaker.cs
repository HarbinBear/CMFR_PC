using UnityEngine;
using System.IO;
using System;


namespace Framework.CMFR
{
    public class ScreenShotTaker: MonoBehaviour
    {
        public string screenshotDirectory = "Screenshots/";

        private void Start()
        {
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) // 使用空格键为触发截图键
            {
                TakeScreenshot();
            }
        }

        public void TakeScreenshot()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string screenshotFileName = screenshotDirectory + "Screenshot_" + timestamp + ".png";
            ScreenCapture.CaptureScreenshot(screenshotFileName);
            Debug.Log("Screenshot saved to: " + screenshotFileName);
        }
    }
}