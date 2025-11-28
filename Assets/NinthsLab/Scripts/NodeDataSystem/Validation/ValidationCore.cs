using System.Collections.Generic;
using System.Text;
using System.Linq;
using System; // 引入 System 以支持 IDisposable

namespace LogicEngine.Validation
{
    /// <summary>
    /// 日志等级枚举
    /// </summary>
    public enum ValidationSeverity
    {
        Info,    // 普通日志 (Log)
        Warning, // 警告 (Warning)
        Error    // 错误 (Error)
    }

    /// <summary>
    /// 单条验证记录
    /// </summary>
    public struct ValidationEntry
    {
        public ValidationSeverity Severity;
        public string Path;
        public string Message;

        public override string ToString()
        {
            // 这里为了在 Unity Console 看得清楚，加了一些格式
            string prefix = Severity switch
            {
                ValidationSeverity.Error => "[ERROR]",
                ValidationSeverity.Warning => "[WARN] ",
                _ => "[INFO] "
            };
            return $"{prefix} [{Path}] {Message}";
        }
    }

    /// <summary>
    /// 所有需要自检的数据类都实现此接口
    /// </summary>
    public interface IValidatable
    {
        void OnValidate(ValidationContext context);
    }

    /// <summary>
    /// 验证结果对象
    /// </summary>
    public class ValidationResult
    {
        // 只有当没有 Error 级别的记录时，才视为验证通过
        public bool IsValid => !Entries.Any(e => e.Severity == ValidationSeverity.Error);
        
        // 存储所有的日志条目
        public List<ValidationEntry> Entries { get; private set; } = new List<ValidationEntry>();

        public void AddEntry(ValidationSeverity severity, string path, string message)
        {
            Entries.Add(new ValidationEntry
            {
                Severity = severity,
                Path = path,
                Message = message
            });
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            if (IsValid)
            {
                sb.AppendLine($"<color=green>Validation Passed.</color>");
            }
            else
            {
                int errorCount = Entries.Count(e => e.Severity == ValidationSeverity.Error);
                sb.AppendLine($"<color=red>Validation Failed ({errorCount} errors).</color>");
            }

            // 如果有任何记录（包括 Info 和 Warning），都打印出来
            if (Entries.Count > 0)
            {
                sb.AppendLine("Details:");
                foreach (var entry in Entries)
                {
                    // 可以根据等级给每一行加颜色 (Unity Rich Text)
                    string color = entry.Severity switch
                    {
                        ValidationSeverity.Error => "red",
                        ValidationSeverity.Warning => "yellow",
                        _ => "white"
                    };
                    sb.AppendLine($"<color={color}>{entry}</color>");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 验证上下文，在递归检查中传递
    /// </summary>
    public class ValidationContext
    {
        private readonly ValidationResult _result = new ValidationResult();
        private readonly Stack<string> _pathStack = new Stack<string>();

        public ValidationResult Result => _result;

        // === 核心日志方法 ===

        public void LogError(string message)
        {
            Record(ValidationSeverity.Error, message);
        }

        public void LogWarning(string message)
        {
            Record(ValidationSeverity.Warning, message);
        }

        public void LogInfo(string message)
        {
            Record(ValidationSeverity.Info, message);
        }

        private void Record(ValidationSeverity severity, string message)
        {
            string currentPath = string.Join(".", _pathStack.Reverse());
            _result.AddEntry(severity, currentPath, message);
        }

        // === 手动作用域管理方法 (新增) ===

        /// <summary>
        /// 手动进入一个新的路径节点。
        /// 注意：必须与 EndScope 成对调用，建议使用 using(Scope(...)) 替代。
        /// </summary>
        /// <param name="scopeName">路径节点名称</param>
        public void BeginScope(string scopeName)
        {
            _pathStack.Push(scopeName);
        }

        /// <summary>
        /// 手动退出当前路径节点。
        /// </summary>
        public void EndScope()
        {
            if (_pathStack.Count > 0)
            {
                _pathStack.Pop();
            }
        }

        /// <summary>
        /// 创建一个自动管理的路径作用域，配合 using 语句块使用。
        /// 示例: using (context.Scope("SubStruct")) { ... }
        /// </summary>
        public IDisposable Scope(string scopeName)
        {
            BeginScope(scopeName);
            return new ValidationScopeToken(this);
        }

        /// <summary>
        /// 用于自动调用 EndScope 的结构体
        /// </summary>
        private readonly struct ValidationScopeToken : IDisposable
        {
            private readonly ValidationContext _context;

            public ValidationScopeToken(ValidationContext context)
            {
                _context = context;
            }

            public void Dispose()
            {
                _context.EndScope();
            }
        }

        // === 递归验证辅助方法 ===

        /// <summary>
        /// 进入子结构进行验证的辅助方法
        /// </summary>
        /// <param name="childName">子结构名称</param>
        /// <param name="target">子结构对象</param>
        /// <param name="allowNull">如果不允许为空且目标为空，报Error；如果允许为空且目标为空，报Info。</param>
        public void ValidateChild(string childName, IValidatable target, bool allowNull = false)
        {
            _pathStack.Push(childName);
            
            if (target == null)
            {
                if (allowNull)
                {
                    // 允许为空，记录一条 Info 即可，不报错
                    LogInfo($"Child object is null (Allowed).");
                }
                else
                {
                    // 不允许为空，记录 Error
                    LogError("Critical: Child object is null.");
                }
            }
            else
            {
                target.OnValidate(this);
            }

            _pathStack.Pop();
        }

        /// <summary>
        /// 验证列表类型的子结构
        /// </summary>
        public void ValidateList<T>(string listName, List<T> list) where T : IValidatable
        {
            _pathStack.Push(listName);
            if (list == null)
            {
                // 这里默认 List 本身不能为 null，如果允许 list 为 null 可以参照 ValidateChild 修改
                LogInfo("List is null (Treated as empty)."); 
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    // 列表里的项通常不允许为 null，这里默认设为 false
                    ValidateChild($"Item_{i}", list[i], allowNull: false);
                }
            }
            _pathStack.Pop();
        }

        // 初始化根路径
        public ValidationContext(string rootName)
        {
            _pathStack.Push(rootName);
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class ValidationExtensions
    {
        public static ValidationResult SelfCheck(this IValidatable target, string rootName = "Root")
        {
            var context = new ValidationContext(rootName);
            if (target == null)
            {
                context.Result.AddEntry(ValidationSeverity.Error, rootName, "Target object is null.");
                return context.Result;
            }

            target.OnValidate(context);
            return context.Result;
        }
    }
}