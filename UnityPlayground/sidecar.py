from pynput import keyboard
import subprocess
import threading

# 记录当前按下的键
current_keys = set()

def toggle_sidecar(device_name="罗勇的iPad"):
    """
    开启/关闭随航（屏幕镜像）
    :param device_name: iPad的名称，默认为"iPad"
    """
    try:
        print("\n尝试打开屏幕镜像...")
        # 点击控制中心并查找显示器选项
        click_script = '''
            on findLastTargetIndex(targetItem, itemList)
                set lastIndex to 0
                repeat with i from (count of itemList) to 1 by -1
                    if item i of itemList is targetItem then
                        set lastIndex to i
                        exit repeat
                    end if
                end repeat
                return lastIndex
            end findLastTargetIndex

            beep 1
            beep 1
            tell application "System Settings"
                activate
                delay 1
                tell application "System Events"
                    tell process "System Settings"
                        click menu item "显示器" of menu "显示" of menu bar item "显示" of menu bar 1
                        delay 0.3
                        tell group 1 of group 2 of splitter group 1 of group 1 of window "显示器"
                            try
                                click pop up button "添加"
                                delay 0.3
                                -- 获取所有菜单项的名称
                                set menuItems to name of menu items of menu "添加" of pop up button "添加"
                                -- 通过名字查找要准确一些，这里去找最后一个名字的索引，因为如果 ipadpro 有妙控键盘，就会出现两个名字，我们需要最后一个名字，第一个名字是连接键鼠的
                                -- set targetIndex to (my findLastTargetIndex("罗勇的iPad", menuItems))
                                set targetIndex to (my findLastTargetIndex("罗勇的MacBook Air", menuItems))
                                
                                -- 点击目标菜单项
                                click menu item targetIndex of menu "添加" of pop up button "添加"
                            on error
                                delay 0.5
                            end try
                        end tell
                    end tell
                end tell
            end tell
            delay 1
            beep 1
            tell application "System Settings" to quit
        '''
        subprocess.run(['osascript', '-e', click_script])
        print("操作完成")
    except Exception as e:
        print(f"执行出错: {e}")

def on_press(key):
    """当键被按下时调用"""
    try:
        # 将按下的键添加到集合中
        current_keys.add(key)
        
        # 检查组合键
        # Command (cmd) = Key.cmd
        # Option (alt) = Key.alt
        # Control = Key.ctrl
        # 方向右 = Key.right
        if (keyboard.Key.cmd in current_keys and 
            keyboard.Key.alt in current_keys and 
            keyboard.Key.ctrl in current_keys and 
            keyboard.Key.right in current_keys):
            
            # 执行随航切换
            toggle_sidecar()
            
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
            print("- Cmd + Option + Control + 方向右：切换随航模式")
            print("按 Ctrl+C 退出程序")
            # 保持程序运行
            listener.join()
    except KeyboardInterrupt:
        print("\n程序已退出")
        return

if __name__ == "__main__":
    main()
