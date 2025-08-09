from pynput import keyboard
import subprocess
import threading

# 记录当前按下的键
current_keys = set()

def execute_pull():
    """执行 git pull 命令"""
    try:
        result = subprocess.run(['git', 'pull'], capture_output=True, text=True)
        print(f"\nGit Pull Output:\n{result.stdout}")
        if result.stderr:
            print(f"Errors/Warnings:\n{result.stderr}")
    except Exception as e:
        print(f"\nError executing git pull: {e}")

def execute_push_sequence():
    """执行 git add、commit 和 push 命令序列"""
    try:
        # git add .
        add_result = subprocess.run(['git', 'add', '.'], capture_output=True, text=True)
        print("\nGit Add Output:")
        if add_result.stderr:
            print(f"Errors/Warnings:\n{add_result.stderr}")
        
        # git commit
        commit_result = subprocess.run(['git', 'commit', '-m', 'no commit'], capture_output=True, text=True)
        print(f"\nGit Commit Output:\n{commit_result.stdout}")
        if commit_result.stderr:
            print(f"Errors/Warnings:\n{commit_result.stderr}")
        
        # git push
        push_result = subprocess.run(['git', 'push'], capture_output=True, text=True)
        print(f"\nGit Push Output:\n{push_result.stdout}")
        if push_result.stderr:
            print(f"Errors/Warnings:\n{push_result.stderr}")
    except Exception as e:
        print(f"\nError executing git commands: {e}")

def on_press(key):
    """当键被按下时调用"""
    try:
        # 将按下的键添加到集合中
        current_keys.add(key)
        
        # 检查组合键
        # Command (cmd) = Key.cmd
        # Option (alt) = Key.alt
        # Control = Key.ctrl
        # 方向键 = Key.down/Key.up
        if (keyboard.Key.cmd in current_keys and 
            keyboard.Key.alt in current_keys and 
            keyboard.Key.ctrl in current_keys):
            
            if keyboard.Key.down in current_keys:
                # 方向下键执行 pull
                execute_pull()
            elif keyboard.Key.up in current_keys:
                # 方向上键执行 add->commit->push
                execute_push_sequence()
            
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
            print("- Cmd + Option + Control + 方向下：执行 git pull")
            print("- Cmd + Option + Control + 方向上：执行 git add . -> commit -> push")
            print("按 Ctrl+C 退出程序")
            # 保持程序运行
            listener.join()
    except KeyboardInterrupt:
        print("\n程序已退出")
        return

if __name__ == "__main__":
    main()
