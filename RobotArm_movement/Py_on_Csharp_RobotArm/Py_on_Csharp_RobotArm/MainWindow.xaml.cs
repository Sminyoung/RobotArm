using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Py_on_Csharp_RobotArm
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Homing_Click(object sender, RoutedEventArgs e)
        {
            //프로세스 파일명 정의
            var psi = new ProcessStartInfo();   // 파이썬 exe를 직접 작동해서 코드 실행
            psi.FileName = @"C:\Users\BIT\AppData\Local\Programs\Python\Python39\python.exe"; //파이썬 설치 경로
            psi.Arguments = $"\"C:\\Users\\BIT\\Desktop\\movement_py\\RobotArm_movement.py\""; //파일경로
            
            //3) Process configuration
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            //4) return value def (에러 및 결과 출력)
            var erros = "";
            var results = "";
            using (var process = Process.Start(psi))
            {
                erros = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();
            }
            Console.WriteLine(erros);
            Console.WriteLine(results);
        }

        private void Pick_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}