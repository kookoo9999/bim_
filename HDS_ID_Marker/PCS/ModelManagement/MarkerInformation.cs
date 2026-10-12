using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PCS.ModelManager
{
    public class MarkerInformation : ProfileNotifyChanged
    {
        public double MarkStartPosX_top;//마킹 시작 X 실제 위치
        public double MarkStartPosY_top;//마킹 시작 Y 실제 위치
        public double MarkStartPosX_bot;//마킹 시작 X 실제 위치
        public double MarkStartPosY_bot;//마킹 시작 Y 실제 위치
        public int MarkPosX_top;//이미지 상에서의 마킹 X위치
        public int MarkPosY_top;//이미지 상에서의 마킹 Y위치
        public int MarkPosX_bot;//이미지 상에서의 마킹 X위치
        public int MarkPosY_bot;//이미지 상에서의 마킹 Y위치

        public int PosX1_top;//이미지 상에서의 frist roi X 위치
        public int PosY1_top;//이미지 상에서의 frist roi Y 위치
        public int PosX1_bot;//이미지 상에서의 frist roi X 위치
        public int PosY1_bot;//이미지 상에서의 frist roi Y 위치
        public int PosX2;//이미지 상에서의 second roi X 위치
        public int PosY2;//이미지 상에서의 second roi Y 위치
        public double CamPosX1_top;//align roi 1 x 실제 위치
        public double CamPosY1_top;//align roi 1 y 실제 위치
        public double CamPosX1_bot;//align roi 1 x 실제 위치
        public double CamPosY1_bot;//align roi 1 x 실제 위치
        public int MatchRate;

        public double AoiCamPosX;
        public double AoiCamPosY;
        public double AoiPosX;
        public double AoiPosY;

        public MarkerInformation Clone()
        {
            MarkerInformation marker = new MarkerInformation();
            marker.MarkStartPosX_top = this.MarkStartPosX_top;
            marker.MarkStartPosY_top = this.MarkStartPosY_top;
            marker.MarkPosX_top = MarkPosX_top;
            marker.MarkPosY_top = MarkPosY_top;

            marker.PosX1_top = this.PosX1_top;
            marker.PosY1_top = this.PosY1_top;
            marker.PosX2 = this.PosX2;
            marker.PosY2 = this.PosY2;
            marker.MatchRate = this.MatchRate;
            marker.CamPosX1_top = this.CamPosX1_top;
            marker.CamPosY1_top = this.CamPosY1_top;
            //marker.CamPosX2 = this.CamPosY1_top;
            //marker.CamPosY2 = this.CamPosY2;
            marker.CamPosX1_bot = this.CamPosX1_bot;
            marker.CamPosY1_bot = this.CamPosY1_bot;

            marker.AoiCamPosX = this.AoiCamPosX;
            marker.AoiCamPosY = this.AoiCamPosY;
            marker.AoiPosX = this.AoiPosX;
            marker.AoiPosY = this.AoiPosY;

            return marker;
        }

        public MarkerInformation()
        {
        }
    }
}
