using System;
using System.Threading.Tasks;

namespace RadzenBlazorDemos
{
    public interface ICompilerService
    {
        Task<Type> CompileAsync(string source);
    }
}


