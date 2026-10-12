//using Common;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PCS.ModelManager
//{
//    public enum ModelOperation
//    {
//        NONE = 0,
//        Create = 1,
//        Modify = 2,
//        Delete = 3,
//        Copy = 4,
//        Paste = 5,
//        Rename = 6
//    }

//    public class ModelManager
//    {
//        public static ModelInformation SelectedModel { get; set; }
//        public ModelInformation CaptureModel { get; set; }

//        public ObservableCollection<ModelInformation> Models
//        {
//            get
//            {
//                return m_listModel;
//            }
//        }

//        private ObservableCollection<ModelInformation> m_listModel = new ObservableCollection<ModelInformation>();

//        public string ModelPath;
//        public string ResultPath;

//        public string CheckModelExist(string aszModelName)
//        {
//            //모델이 존재하지 않음
//            if (!Directory.Exists(ModelPath))
//                return string.Empty;

//            string[] path = Directory.GetDirectories(ModelPath);
//            int nIndex = 0;

//            foreach (string s in path)
//            {
//                if (File.Exists(s + "\\modelinfo.ini"))
//                {
//                    ModelInformation modelInfo = new ModelInformation();
//                    modelInfo.Index = ++nIndex;
//                    IniFile ini = new IniFile(s + "\\modelinfo.ini");
//                    modelInfo.Name = ini.ReadString("Model", "Name", "");
//                    if (aszModelName == modelInfo.Name)
//                        return modelInfo.Name;
//                }
//            }

//            return string.Empty;
//        }

//        public static bool UpdateModelMarkInfo(string path, ModelInformation model)
//        {
//            if (!Directory.Exists(path))
//                Directory.CreateDirectory(path);
//            if (!File.Exists(path + "\\modelinfo.ini"))
//            {
//                FileStream fs = File.Create(path + "\\modelinfo.ini");
//                fs.Close();
//            }

//            IniFile ini = new IniFile(path + "\\modelinfo.ini");

//            ini.WriteDouble("Marker", "MarkStartPosX", model.Marker.MarkStartPosX_top);
//            ini.WriteDouble("Marker", "MarkStartPosY", model.Marker.MarkStartPosY_top);
//            ini.WriteInteger("Marker", "PosX1_top", model.Marker.PosX1_top);
//            ini.WriteInteger("Marker", "PosX1_bot", model.Marker.PosX1_bot);
//            ini.WriteInteger("Marker", "PosY1_top", model.Marker.PosY1_top);
//            ini.WriteInteger("Marker", "PosY1_bot", model.Marker.PosY1_bot);

//            ini.WriteInteger("Marker", "MarkPosX", model.Marker.MarkPosX_top);
//            ini.WriteInteger("Marker", "MarkPosY", model.Marker.MarkPosY_top);

//            ini.WriteDouble("Marker", "CamPosX1_top", model.Marker.CamPosX1_top);
//            ini.WriteDouble("Marker", "CamPosY1_top", model.Marker.CamPosY1_top);
//            ini.WriteDouble("Marker", "CamPosX1_bot", model.Marker.CamPosX1_bot);
//            ini.WriteDouble("Marker", "CamPosY1_bot", model.Marker.CamPosY1_bot);
//            ini.WriteInteger("Marker", "Match", model.Marker.MatchRate);

//            ini.WriteDouble("Marker", "AoiCamPosX", model.Marker.AoiCamPosX);
//            ini.WriteDouble("Marker", "AoiCamPosY", model.Marker.AoiCamPosY);
//            ini.WriteDouble("Marker", "AoiPosX", model.Marker.AoiPosX);
//            ini.WriteDouble("Marker", "AoiPosY", model.Marker.AoiPosY);

//            SelectedModel = model;

//            return true;
//        }

//        public bool ModifyModel(ModelInformation modifiedModel)
//        {
//            if (!Directory.Exists(ModelPath + "\\" + modifiedModel.Name))
//                Directory.CreateDirectory(ModelPath + "\\" + modifiedModel.Name);
//            if (!File.Exists(ModelPath + "\\" + modifiedModel.Name + "\\modelinfo.ini"))
//            {
//                FileStream fs = File.Create(ModelPath + "\\" + modifiedModel.Name + "\\modelinfo.ini");
//                fs.Close();
//            }

//            IniFile ini = new IniFile(ModelPath + "\\" + modifiedModel.Name + "\\modelinfo.ini");
//            ini.WriteString("Model", "Name", modifiedModel.Name);
//            ini.WriteDouble("Model", "StripPitch", modifiedModel.StripPitch);
//            ini.WriteDouble("Model", "StripHeight", modifiedModel.StripHeight);
//            ini.WriteDouble("Model", "StripWidth", modifiedModel.StripWidth);
//            ini.WriteInteger("Model", "NumStrip", modifiedModel.NumStrip);
//            ini.WriteInteger("Model", "Customer", modifiedModel.Customer);
//            //ini.WriteBool("Model", "MarkingBoth", modifiedModel.MarkingBoth);

//            ini.WriteInteger("Model", "ColumnRow", modifiedModel.ColumnRow);
//            //ini.WriteDouble("Model", "MarkWidth", modifiedModel.MarkWidth);
//            //ini.WriteDouble("Model", "MarkHeight", modifiedModel.MarkHeight);
//            ini.WriteBool("Model", "UseAOIMark", modifiedModel.UseAOIMark);

//            //string l1, l2;
//            //l1 = l2 = "";
//            //for (int i = 0; i < 4; i++)
//            //{
//            //    l1 += modifiedModel.LightValue1[i].ToString() + ",";
//            //    l2 += modifiedModel.LightValue2[i].ToString() + ",";
//            //}
//            //ini.WriteString("Light1", "Value", l1);
//            //ini.WriteString("Light2", "Value", l2);

//            ini.WriteString("Light1", "Value", modifiedModel.LightValue1);
//            ini.WriteString("Light2", "Value", modifiedModel.LightValue2);
//            ini.WriteString("Light3", "Value", modifiedModel.LightValue3);

//            for (int i = 0; i < m_listModel.Count; i++)
//            {
//                if (modifiedModel.Name == m_listModel[i].Name)
//                {
//                    m_listModel[i] = modifiedModel;
//                    break;
//                }
//            }
//            return true;
//        }

//        public void LoadUsedModelNames(ref List<string> listStoredModelName)
//        {
//            if (!Directory.Exists(ModelPath)) return;

//            listStoredModelName.Clear();

//            string[] path = Directory.GetDirectories(ModelPath);
//            int nIndex = 0;
//            foreach (string s in path)
//            {
//                if (File.Exists(s + "\\modelinfo.ini"))
//                {
//                    ModelInformation modelInfo = new ModelInformation();
//                    modelInfo.Index = ++nIndex;
//                    IniFile ini = new IniFile(s + "\\modelinfo.ini");
//                    modelInfo.Name = ini.ReadString("Model", "Name", "");
//                    listStoredModelName.Add(modelInfo.Name);
//                }
//            }
//        }

//        public void LoadModel()
//        {
//            if (!Directory.Exists(ModelPath)) return;

//            m_listModel.Clear();

//            string[] path = Directory.GetDirectories(ModelPath);
//            int nIndex = 0;

//            foreach (string s in path)
//            {
//                if (File.Exists(s + "\\modelinfo.ini"))
//                {
//                    ModelInformation modelInfo = new ModelInformation();
//                    modelInfo.Index = ++nIndex;

//                    IniFile ini = new IniFile(s + "\\modelinfo.ini");
//                    modelInfo.Name = ini.ReadString("Model", "Name", "");
//                    modelInfo.StripPitch = ini.ReadDouble("Model", "StripPitch", 35);
//                    modelInfo.StripHeight = ini.ReadDouble("Model", "StripHeight", 0.0);
//                    modelInfo.StripWidth = ini.ReadDouble("Model", "StripWidth", 0.0);
//                    modelInfo.NumStrip = ini.ReadInteger("Model", "NumStrip", 4);
//                    modelInfo.Customer = ini.ReadInteger("Model", "Customer",0);
//                    //modelInfo.MarkingBoth = ini.ReadBool("Model", "MarkingBoth", true);

//                    modelInfo.ColumnRow = ini.ReadInteger("Model", "ColumnRow", 1);
//                    //modelInfo.MarkWidth = ini.ReadDouble("Model", "MarkWidth", 10);
//                    //modelInfo.MarkHeight = ini.ReadDouble("Model", "MarkHeight", 10);
//                    modelInfo.UseAOIMark = ini.ReadBool("Model", "UseAOIMark", false);

//                    //string[] light1 = ini.ReadString("Light1", "Value", "0,0,0,0").Split(',');
//                    //string[] light2 = ini.ReadString("Light2", "Value", "0,0,0,0").Split(',');
//                    //if (light1.Length >= 4 && light2.Length >= 4)
//                    //{
//                    //    for (int i = 0; i < 4; i++)
//                    //    {
//                    //        try
//                    //        {
//                    //            modelInfo.LightValue1[i] = Convert.ToInt32(light1[i]);
//                    //            modelInfo.LightValue2[i] = Convert.ToInt32(light2[i]);
//                    //        }
//                    //        catch
//                    //        {
//                    //            modelInfo.LightValue1[i] = modelInfo.LightValue2[i] = 0;
//                    //        }
//                    //    }
//                    //}

//                    modelInfo.LightValue1 = ini.ReadString("Light1", "Value", "0");
//                    modelInfo.LightValue2 = ini.ReadString("Light2", "Value", "0");
//                    modelInfo.LightValue3 = ini.ReadString("Light3", "Value", "0");

//                    modelInfo.Marker.MarkStartPosX_top = ini.ReadDouble("Marker", "MarkStartPosX", 0);
//                    modelInfo.Marker.MarkStartPosY_top = ini.ReadDouble("Marker", "MarkStartPosY", 0);
//                    modelInfo.Marker.PosX1_top = ini.ReadInteger("Marker", "PosX1_top", 0);
//                    modelInfo.Marker.PosX1_bot = ini.ReadInteger("Marker", "PosX1_bot", 0);
//                    modelInfo.Marker.PosY1_top = ini.ReadInteger("Marker", "PosY1_top", 0);
//                    modelInfo.Marker.PosY1_bot = ini.ReadInteger("Marker", "PosY1_bot", 0);
//                    modelInfo.Marker.CamPosX1_top = ini.ReadDouble("Marker", "CamPosX1_top", 0);
//                    //modelInfo.Marker.CamPosX2 = ini.ReadDouble("Marker", "CamPosX2", 0);
//                    modelInfo.Marker.CamPosY1_top = ini.ReadDouble("Marker", "CamPosY1_top", 0);
//                    //modelInfo.Marker.CamPosY2 = ini.ReadDouble("Marker", "CamPosY2", 0);
//                    modelInfo.Marker.CamPosX1_bot = ini.ReadDouble("Marker", "CamPosX1_bot", 0);
//                    modelInfo.Marker.CamPosY1_bot = ini.ReadDouble("Marker", "CamPosY1_bot", 0);
//                    modelInfo.Marker.MatchRate = ini.ReadInteger("Marker", "Match", 60);

//                    modelInfo.Marker.MarkPosX_top = ini.ReadInteger("Marker", "MarkPosX", 0);
//                    modelInfo.Marker.MarkPosY_top = ini.ReadInteger("Marker", "MarkPosY", 0);

//                    modelInfo.Marker.AoiCamPosX = ini.ReadDouble("Marker", "AoiCamPosX", 0);
//                    modelInfo.Marker.AoiCamPosY = ini.ReadDouble("Marker", "AoiCamPosY", 0);
//                    modelInfo.Marker.AoiPosX = ini.ReadDouble("Marker", "AoiPosX", 0);
//                    modelInfo.Marker.AoiPosY = ini.ReadDouble("Marker", "AoiPosY", 0);

//                    m_listModel.Add(modelInfo);
//                }
//            }
//        }

//        public bool CreateModel(ModelInformation newModel)
//        {
//            //해당 모델 폴더 없을경우 폴더 생성
//            if (!Directory.Exists(ModelPath + "\\" + newModel.Name))
//                Directory.CreateDirectory(ModelPath + "\\" + newModel.Name);
//            //해당 모델 ini 없을경우 ini 생성
//            if (!File.Exists(ModelPath + "\\" + newModel.Name + "\\modelinfo.ini"))
//            {
//                FileStream fs = File.Create(ModelPath + "\\" + newModel.Name + "\\modelinfo.ini");
//                fs.Close();
//            }

//            IniFile ini = new IniFile(ModelPath + "\\" + newModel.Name + "\\modelinfo.ini");
//            newModel.Index = m_listModel.Count;

//            ini.WriteString("Model", "Name", newModel.Name);
//            ini.WriteDouble("Model", "StripPitch", newModel.StripPitch);
//            ini.WriteDouble("Model", "StripHeight", newModel.StripHeight);
//            ini.WriteDouble("Model", "StripWidth", newModel.StripWidth);
//            ini.WriteInteger("Model", "NumStrip", newModel.NumStrip);
//            ini.WriteInteger("Model", "Customer", newModel.Customer);
//            //ini.WriteBool("Model", "MarkingBoth", newModel.MarkingBoth);

//            ini.WriteInteger("Model", "ColumnRow", newModel.ColumnRow);
//            //ini.WriteDouble("Model", "MarkWidth", newModel.MarkWidth);
//            //ini.WriteDouble("Model", "MarkHeight", newModel.MarkHeight);
//            ini.WriteBool("Model", "UseAOIMark", newModel.UseAOIMark);

//            //string l1, l2;
//            //l1 = l2 = "";
//            //for (int i = 0; i < 4; i++)
//            //{
//            //    l1 += newModel.LightValue1[i].ToString() + ",";
//            //    l2 += newModel.LightValue2[i].ToString() + ",";
//            //}
//            ini.WriteString("Light1", "Value", newModel.LightValue1);
//            ini.WriteString("Light2", "Value", newModel.LightValue2);
//            ini.WriteString("Light3", "Value", newModel.LightValue3);

//            ini.WriteDouble("Marker", "MarkStartPosX", newModel.Marker.MarkStartPosX_top);
//            ini.WriteDouble("Marker", "MarkStartPosY", newModel.Marker.MarkStartPosY_top);
//            ini.WriteInteger("Marker", "PosX1_top", newModel.Marker.PosX1_top);
//            ini.WriteInteger("Marker", "PosX1_bot", newModel.Marker.PosX1_bot);
//            ini.WriteInteger("Marker", "PosY1_top", newModel.Marker.PosY1_top);
//            ini.WriteInteger("Marker", "PosY1_bot", newModel.Marker.PosY1_bot);

//            ini.WriteDouble("Marker", "CamPosX1_top", newModel.Marker.CamPosX1_top);
//            //ini.WriteDouble("Marker", "CamPosX2", newModel.Marker.CamPosX2);
//            ini.WriteDouble("Marker", "CamPosY1_top", newModel.Marker.CamPosY1_top);
//            //ini.WriteDouble("Marker", "CamPosY2", newModel.Marker.CamPosY2);
//            ini.WriteDouble("Marker", "CamPosX1_bot", newModel.Marker.CamPosX1_bot);
//            ini.WriteDouble("Marker", "CamPosY1_bot", newModel.Marker.CamPosY1_bot);
//            ini.WriteInteger("Marker", "Match", newModel.Marker.MatchRate);

//            ini.WriteInteger("Marker", "MarkPosX", newModel.Marker.MarkPosX_top);
//            ini.WriteInteger("Marker", "MarkPosY", newModel.Marker.MarkPosY_top);

//            ini.WriteDouble("Marker", "AoiCamPosX", newModel.Marker.AoiCamPosX);
//            ini.WriteDouble("Marker", "AoiCamPosY", newModel.Marker.AoiCamPosY);
//            ini.WriteDouble("Marker", "AoiPosX", newModel.Marker.AoiPosX);
//            ini.WriteDouble("Marker", "AoiPosY", newModel.Marker.AoiPosY);

//            m_listModel.Add(newModel);
//            return true;
//        }

//        public static ModelInformation CreateCloneModel(ModelInformation sourceModel)
//        {
//            ModelInformation cloneModel = new ModelInformation();

//            cloneModel.Code = sourceModel.Code;
//            cloneModel.Name = sourceModel.Name;
//            cloneModel.Index = sourceModel.Index;
//            cloneModel.StripHeight = sourceModel.StripHeight;
//            cloneModel.StripWidth = sourceModel.StripWidth;
//            cloneModel.NumStrip = sourceModel.NumStrip;
//            cloneModel.Customer = sourceModel.Customer;
//            //cloneModel.MarkingBoth = sourceModel.MarkingBoth;
//            cloneModel.StripPitch = sourceModel.StripPitch;

//            cloneModel.ColumnRow = sourceModel.ColumnRow;
//            //cloneModel.MarkWidth = sourceModel.MarkWidth;
//            //cloneModel.MarkHeight = sourceModel.MarkHeight;
//            cloneModel.UseAOIMark = sourceModel.UseAOIMark;

//            cloneModel.LightValue1 = sourceModel.LightValue1;
//            cloneModel.LightValue2 = sourceModel.LightValue2;
//            cloneModel.LightValue3 = sourceModel.LightValue3;

//            cloneModel.Marker = sourceModel.Marker.Clone();

//            return cloneModel;
//        }

//        public bool DeleteModel(ModelInformation selectedModel)
//        {
//            //폴더 없음
//            if (!Directory.Exists(ModelPath + "\\" + selectedModel.Name))
//                return true;
//            //파일 삭제
//            string[] files = Directory.GetFiles(ModelPath + "\\" + selectedModel.Name);
//            foreach (string f in files)
//            {
//                File.Delete(f);
//            }
//            //폴더 삭제
//            if (Directory.Exists(ModelPath + "\\" + selectedModel.Name))
//            {
//                Directory.Delete(ModelPath + "\\" + selectedModel.Name);
//            }
//            //모델 목록에서 삭제
//            for (int i = 0; i < m_listModel.Count; i++)
//            {
//                if (selectedModel.Name == m_listModel[i].Name)
//                {
//                    m_listModel.Remove(selectedModel);
//                    break;
//                }
//            }
//            return true;
//        }
//        public bool RenameModel(int nIndex, string strSrcName, string strNewName)
//        {
//            if (!File.Exists(ModelPath + "\\" + strSrcName + "\\modelinfo.ini"))//modelinfo file 없을경우
//                return false;
//            IniFile ini = new IniFile(ModelPath + "\\" + strSrcName + "\\modelinfo.ini");
//            ini.WriteString("Model", "Name", strNewName);

//            DirectoryManager.MoveDirectory(ModelPath + "\\" + strSrcName, ModelPath + "\\" + strNewName);
//            for (int i = 0; i < m_listModel.Count; i++)
//            {
//                if (strSrcName == m_listModel[i].Name)
//                {
//                    m_listModel[i].Name = strNewName;
//                    break;
//                }
//            }
//            return true;
//        }
//    }
//}
