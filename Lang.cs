using System.Collections.Generic;

namespace SCLaunch;

public static class Lang
{
    public static string Current = "zh";

    private static readonly Dictionary<string, string> Strings = new();

    private static readonly (string Key, string Zh, string En, string Ru)[] All = new (string, string, string, string)[]
    {
        ("discover", "发现", "Discover", "Обзор"),
        ("friend_links", "友情链接", "Links", "Ссылки"),
        ("extract_file", "解压文件", "Extract", "Распаковать"),
        ("edit", "编辑", "Edit", "Изменить"),
        ("delete", "删除", "Delete", "Удалить"),
        ("add_path", "添加游戏", "Add Game", "Добавить игру"),
        ("not_sc_game", "该文件夹未检测到 Survivalcraft 相关文件，请确认选择了正确的游戏目录。", "No Survivalcraft files detected. Make sure you selected the correct game folder.", "Файлы Survivalcraft не найдены. Убедитесь, что вы выбрали правильную папку."),
        ("hint_step", "1. 点击左下角 \"添加游戏\" > 2. 选择游戏.exe 应用程序", "1. Click \"Add Game\" at bottom-left > 2. Select a game .exe", "1. Нажмите \"Добавить игру\" внизу > 2. Выберите .exe файл"),
        ("hint_note", "注意：本程序为测试项目", "Note: This is a test project", "Примечание: это тестовый проект"),
        ("download_version", "下载版本", "Download", "Загрузить"),
        ("download_list", "下载列表", "Tasks", "Задачи"),
        ("type", "类型：", "Type:", "Тип:"),
        ("source", "源：", "Source:", "Источник:"),
        ("download_tasks", "下载任务", "Downloads", "Загрузки"),
        ("open_folder", "打开文件夹", "Open Folder", "Открыть папку"),
        ("location", "位置：", "Location:", "Путь:"),
        ("save_to", "保存到：", "Save to:", "Сохранить в:"),
        ("none_selected", "未选择", "Not selected", "Не выбрано"),
        ("select", "选择", "Select", "Выбрать"),
        ("download", "下载", "Download", "Загрузить"),
        ("cancel", "取消", "Cancel", "Отмена"),
        ("browser", "浏览器", "Browser", "Браузер"),
        ("launch", "SC-启动！", "GO!", "Запуск!"),
        ("ready", "准备下载...", "Ready...", "Готово..."),
        ("connecting", "连接中...", "Connecting...", "Подключение..."),
        ("paused", "已暂停", "Paused", "На паузе"),
        ("stopped", "已停止", "Stopped", "Остановлено"),
        ("done", "完成", "Done", "Готово"),
        ("extracting", "解压中...", "Extracting...", "Распаковка..."),
        ("extract_done", "解压完成", "Extracted", "Распаковано"),
        ("extract_fail", "解压失败", "Extract failed", "Ошибка распаковки"),
        ("success", "成功", "Done", "Готово"),
        ("download_complete", "下载完成！", "Download complete!", "Загрузка завершена!"),
        ("extract_prompt", "是否解压到同目录？", "Extract to the same folder?", "Распаковать в эту папку?"),
        ("extract_title", "解压", "Extract", "Распаковка"),
        ("extract_complete_msg", "解压完成！", "Done!", "Готово!"),
        ("extract_fail_msg", "解压失败，请确认文件完整或 7z 组件可用。", "Extraction failed. Make sure the file is intact and 7z is available.", "Ошибка распаковки. Убедитесь, что файл целый и 7z доступен."),
        ("error", "错误", "Error", "Ошибка"),
        ("loading", "加载中...", "Loading...", "Загрузка..."),
        ("no_versions", "暂无版本", "No versions available", "Версий пока нет"),
        ("load_failed", "加载失败：", "Failed to load: ", "Не удалось загрузить: "),
        ("failed", "失败", "Failed", "Ошибка"),
        ("confirm_delete", "确认删除", "Delete?", "Удалить?"),
        ("confirm_delete_msg", "确定要删除此版本吗？", "Are you sure you want to delete this version?", "Вы уверены, что хотите удалить эту версию?"),
        ("confirm", "确定", "OK", "Да"),
        ("edit_version", "编辑版本", "Edit Version", "Редактировать версию"),
        ("name_label", "名称：", "Name:", "Название:"),
        ("repo_label", "GitHub 仓库 (owner/repo)：", "GitHub repo (owner/repo):", "Репозиторий (owner/repo):"),
        ("add_version", "增加版本", "Add Version", "Добавить версию"),
        ("select_folder", "选择文件夹", "Select Folder", "Выбрать папку"),
        ("links_title", "友情链接", "Useful Links", "Полезные ссылки"),
        ("net_label", "Survivalcraft Net (联机版)", "Survivalcraft Net (Multiplayer)", "Survivalcraft Net (Мультиплеер)"),
        ("api_label", "Survivalcraft API (模组版)", "Survivalcraft API (Modded)", "Survivalcraft API (Моды)"),
        ("official", "官网", "Official Site", "Официальный сайт"),
        ("community", "生存战争玩家社区（注意真假网站）", "Player Community (beware of fakes)", "Сообщество игроков (осторожно с подделками)"),
        ("sckey", "SCkey服务器管理", "SCkey Server", "SCkey Сервер"),
        ("modsite", "生存战争清凝模组网", "SC Mods", "SC Моды"),
        ("versions_col", "生存战争版本合集（注意安全）", "SC Versions (be careful)", "Версии SC (осторожно)"),
        ("versions_col_label", "生存战争版本合集", "SC Versions", "Версии SC"),
        ("global_mods", "全球模组网（海外）", "Global Mods (overseas)", "Моды из-за рубежа"),
        ("sc_network", "生存战争网（支持三语言/半死状态）", "SC Network (tri-lang, barely alive)", "SC Сеть (3 языка, почти не работает)"),
        ("scdev_label", "survivalcraft.dev（仅英文）", "survivalcraft.dev (English only)", "survivalcraft.dev (только на английском)"),
        ("vk_label", "VK（俄语SC社区）", "VK (Russian SC community)", "VK (русскоязычное сообщество)"),
        ("tapatalk_label", "Tapatalk（原版英文社区）", "Tapatalk (English community)", "Tapatalk (англоязычное сообщество)"),
        ("warn_title", "⚠ SC社区提醒", "⚠ Community Warning", "⚠ Внимание"),
        ("warn_text", "工业锰锌罪状：泄露内测模组，辱骂api开发组，人身攻击多个模组作者，造谣sc抄袭mc，并贬低整个sc圈，在sc各qq群挑事。\n\n注意，此人经常开小号换马甲，已知其用过的网名有：工业解说员，TNT，工业锰锌，七氧化二锰，二氧化碳灭火器，十硝基甲苯等。\n\n另外，此人历来出产过大量劣质模组，如\u201c七彩至臻\u201d\u201c粒子传送门发射器\u201d及许多小修改类mod。鉴于其反社会性格和令人窒息的智力水平，望各位玩家注意甄别。",
            "This user has leaked beta mods, harassed API developers, attacked mod creators, spread false rumors, and caused drama across SC communities.\n\nKnown aliases: 工业解说员, TNT, 工业锰锌, 七氧化二锰, 二氧化碳灭火器, 十硝基甲苯, etc.\n\nKnown for producing low-quality mods. Please be cautious.",
            "Этот пользователь сливал бета-моды, травил разработчиков API, нападал на авторов модов, распускал слухи и провоцировал конфликты в SC-сообществах.\n\nИзвестные аккаунты: 工业解说员, TNT, 工业锰锌, 七氧化二锰, 二氧化碳灭火器, 十硝基甲苯 и др.\n\nИзвестен низкокачественными модами. Будьте осторожны."),
        ("warn_link", "工业锰锌罪状帖子: ", "More details: ", "Подробнее: "),
        ("lang_title", "选择语言", "Select Language", "Выберите язык"),
        ("lang_prompt", "首次启动，请选择语言", "First launch — pick a language", "Первый запуск — выберите язык"),
        ("lang_confirm", "确认", "OK", "Ок"),
        ("type_net", "Net", "Net", "Net"),
        ("type_api", "API", "API", "API"),
        ("about", "关于", "About", "О программе"),
        ("launcher_repo", "SC Launch 仓库", "SC Launch Repository", "Репозиторий SC Launch"),
    };

    static Lang()
    {
        foreach (var (key, zh, en, ru) in All)
            Strings[key] = zh;
    }

    public static void Set(string lang)
    {
        Current = lang;
        foreach (var (key, zh, en, ru) in All)
            Strings[key] = lang switch
            {
                "en" => en,
                "ru" => ru,
                _ => zh
            };
    }

    public static string T(string key) => Strings.TryGetValue(key, out var v) ? v : key;
}