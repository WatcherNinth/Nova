import yaml
import re
import os
from pathlib import Path

exceptedLevel = {
    "TestLevel",
}

# Python相对路径计算规则详解：
# 1. 基于执行时的当前工作目录（CWD），非脚本文件所在目录
# 2. 支持跨平台的路径符转换
# 3. 建议使用os.path处理路径相关操作

project_root_path = os.path.normpath(os.path.join(
        os.path.dirname(os.path.abspath(__file__)),  # 当前脚本所在目录：Tools/NinthsLab
        os.pardir,                         # 上溯到 Tools 目录
        os.pardir,                          # 再上溯到项目根目录
        "Assets"
    ))

level_path = os.path.normpath(os.path.join(
        project_root_path,
        "Resources",
        "InterrorgationLevels",
    ))

script_path = os.path.normpath(os.path.join(
        project_root_path,
        "Resources",
        "Scenarios",
        "InterrogationLevels",
    ))


def get_level_folders():
    """获取需要处理的关卡文件夹列表"""
    return [
        entry for entry in os.listdir(level_path)
        if os.path.isdir(os.path.join(level_path, entry))
        and entry not in exceptedLevel
    ]
def extract_dialogue_values(file_path):
    print(file_path)
    # 预处理YAML文件内容
    content = Path(file_path).read_text(encoding='utf-8')
    
    # 正则处理步骤：
    # 1. 移除TAG声明行和Unity特定标识
    # 2. 清理可能干扰解析的注释和特殊语法
    cleaned_lines = []
    for line in content.split('\n'):
        # 过滤掉TAG声明行和Unity特殊注释行
        if line.startswith('%') or line.strip().startswith('!u!') or line.startswith('---'):
            continue
        # 移除行内的Unity对象引用标记（如 &11400000）
        cleaned_line = re.sub(r'&\d+', '', line)
        cleaned_lines.append(cleaned_line)
    
    cleaned_content = '\n'.join(cleaned_lines)
    
    # 加载处理后的YAML内容
    data = yaml.safe_load(cleaned_content)
    
    # 用于存储结果的列表
    dialogues = []
    
    # 递归查找函数
    def recursive_search(obj):
        if isinstance(obj, dict):
            for k, v in obj.items():
                # 检查是否符合以Dialogue结尾的键名要求
                if re.fullmatch(r'.*Dialogue$', k):
                    if isinstance(v, str):
                        dialogues.append(v)
                    elif isinstance(v, list):
                        dialogues.extend(v)
                recursive_search(v)
        elif isinstance(obj, list):
            for item in obj:
                recursive_search(item)
    
    recursive_search(data)
    return dialogues


def natural_sort_key(s):
    # 分离字符串中的数字和非数字部分
    return [int(text) if text.isdigit() else text.lower() for text in re.split('(\d+)', s)]

def generate_script_content(asset_files, folder_path):
    """生成剧本内容的内部方法"""
    script_content = []
    # 修改后排序逻辑: 1.level.asset排最前 2.含Topic的次之 3.含Proof的再次之 4.自然排序
    sorted_files = sorted(
        asset_files,
        key=lambda x: (
            x.lower() != 'level.asset',  # 第一优先级
            not x.lower().startswith("topic"),
            "proof" in x.lower(),
            natural_sort_key(x)
        )
    )

    dialogues = []

    for asset_file in sorted_files:
        file_path = os.path.join(folder_path, asset_file)
        dialogues.extend(extract_dialogue_values(file_path))
    for d in dialogues:
            script_content.append(f"@<| label '{d}' |>")
            script_content.append("这里放对话")
            script_content.append("")
            script_content.append(f"<| __Nova.deductionManager:DialogueFinished('{d}') |>")
            script_content.append("@<| is_end() |>")
            script_content.append("")
    return script_content


def generate_level_scripts():
    """主处理逻辑"""
    for level_folder in get_level_folders():
        folder_path = os.path.join(level_path, level_folder)

        # 获取所有需要处理的asset文件
        asset_files = [
            f for f in os.listdir(folder_path)
            if f.endswith(".asset")
            and ("Topic" in f or "Proof" in f or f == "level.asset")
        ]

        # 生成剧本内容
        script_content = generate_script_content(asset_files, folder_path)

        # 输出到脚本文件
        output_path = os.path.join(script_path, f"{level_folder}_Script.txt")
        with open(output_path, "w", encoding="utf-8") as f:
            f.write("\n".join(script_content))
        print(f"已生成关卡脚本：{os.path.basename(output_path)}")


if __name__ == "__main__":
    # 确保输出目录存在
    os.makedirs(script_path, exist_ok=True)
    # 执行生成逻辑
    generate_level_scripts()
