from pynput import keyboard
import subprocess
import threading

# 记录当前按下的键
current_keys = set()

def execute_command():
    """执行 pwd 命令"""
    try:
        result = subprocess.run(['git pull'], capture_output=True, text=True)
        print(f"\nCommand output:\n{result.stdout}")
    except Exception as e:
        print(f"\nError executing command: {e}")

def on_press(key):
    """当键被按下时调用"""
    try:
        # 将按下的键添加到集合中
        current_keys.add(key)
        
        # 检查是否按下了所需的组合键
        # Command (cmd) = Key.cmd
        # Option (alt) = Key.alt
        # Control = Key.ctrl
        # 方向下 = Key.down
        if (keyboard.Key.cmd in current_keys and 
            keyboard.Key.alt in current_keys and 
            keyboard.Key.ctrl in current_keys and 
            keyboard.Key.down in current_keys):
            
            execute_command()
            
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
            print("按下 Cmd + Option + Control + 方向下 键来执行 pwd 命令")
            print("按 Ctrl+C 退出程序")
            # 保持程序运行
            listener.join()
    except KeyboardInterrupt:
        print("\n程序已退出")
        return

if __name__ == "__main__":
    main()
