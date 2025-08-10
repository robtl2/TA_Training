from pynput import keyboard
import os
import shutil

def file_sync():
    """
    将源文件夹中更新或新增的文件同步到目标文件夹。
    """
    source_folder = os.path.expanduser("~/Desktop/git/TA_Training/UnityPlayground/Assets")
    dest_folder = "/Volumes/git/TA_Training/UnityPlayground/Assets"

    print("开始同步文件至 macMini")
    
    # 如果目标文件夹不存在，则创建它
    if not os.path.exists(dest_folder):
        print(f"目标文件夹不存在，创建中: {dest_folder}")
        # 首次同步时，使用 shutil.ignore_patterns 忽略隐藏文件
        shutil.copytree(source_folder, dest_folder, ignore=shutil.ignore_patterns('.*', '*~'))
        print("首次同步完成。")
        return

    # 遍历源文件夹中的所有内容
    count = 0
    for root, dirs, files in os.walk(source_folder):
        # 构造当前在目标文件夹中的对应相对路径
        relative_path = os.path.relpath(root, source_folder)
        dest_path = os.path.join(dest_folder, relative_path)

        # 过滤掉以 . 开头的隐藏目录
        dirs[:] = [d for d in dirs if not d.startswith('.')]
        
        # 在目标文件夹中创建对应的子目录
        if not os.path.exists(dest_path):
            print(f"创建目录: {relative_path}")
            os.makedirs(dest_path)
        
        # 遍历当前目录下的所有文件
        for file_name in files:
            # 增加判断条件，如果文件名以 . 开头，则跳过
            if file_name.startswith('.'):
                continue

            source_file = os.path.join(root, file_name)
            dest_file = os.path.join(dest_path, file_name)

            # 检查文件是否需要同步
            try:
                # 如果目标文件不存在
                if not os.path.exists(dest_file):
                    print(f"目标文件不存在，同步: {os.path.join(relative_path, file_name)}")
                    count += 1
                    shutil.copy2(source_file, dest_file)
                else:
                    # 获取源文件和目标文件的修改时间及大小
                    source_mtime = os.path.getmtime(source_file) - 1
                    dest_mtime = os.path.getmtime(dest_file)
                    source_size = os.path.getsize(source_file)
                    dest_size = os.path.getsize(dest_file)

                    # 只有当源文件比目标文件新，或者文件大小不一致时才同步
                    if source_mtime > dest_mtime or source_size != dest_size:
                        print(f"同步文件: {os.path.join(relative_path, file_name)}")
                        count += 1
                        shutil.copy2(source_file, dest_file)

            except Exception as e:
                print(f"同步文件 {os.path.join(relative_path, file_name)} 时出错: {e}")

    print(f"文件同步完成，共同步了 {count} 个文件。")


# 记录当前按下的键
current_keys = set()

def on_press(key):
    """当键被按下时调用"""
    try:
        # 将按下的键添加到集合中
        current_keys.add(key)
        
        # 检查组合键
        # Command (cmd) = Key.cmd
        # Option (alt) = Key.alt
        # Control = Key.ctrl
        # 方向左 = Key.left
        if (keyboard.Key.cmd in current_keys and 
            keyboard.Key.alt in current_keys and 
            keyboard.Key.ctrl in current_keys and 
            keyboard.Key.left in current_keys):
            
            # 执行文件同步
            file_sync()
            
    except AttributeError:
        pass

def on_release(key):
    """当键被释放时调用"""
    try:
        # 从集合中移除释放的键
        current_keys.discard(key)
    except KeyError:
        pass

def main():
    try:
        # 创建监听器
        with keyboard.Listener(on_press=on_press, on_release=on_release) as listener:
            print("键盘监听已启动...")
            print("快捷键说明：")
            print("- Cmd + Option + Control + 方向左：同步文件至macMini")
            print("按 Ctrl+C 退出程序")
            # 保持程序运行
            listener.join()
    except KeyboardInterrupt:
        print("\n程序已退出")
        return

if __name__ == "__main__":
    main()
