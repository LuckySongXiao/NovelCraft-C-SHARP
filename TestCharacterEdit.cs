using System;
using System.Threading.Tasks;
using NovelManagement.Tests;

namespace NovelManagement.TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("角色编辑功能修复验证测试");
            Console.WriteLine("========================================");
            
            var test = new CharacterEditTest();
            bool result = await test.RunAllTestsAsync();
            
            Console.WriteLine("========================================");
            Console.WriteLine($"测试结果: {(result ? "成功" : "失败")}");
            
            if (result)
            {
                Console.WriteLine("✅ 角色编辑跟踪冲突问题已修复！");
            }
            else
            {
                Console.WriteLine("❌ 角色编辑功能仍存在问题");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
