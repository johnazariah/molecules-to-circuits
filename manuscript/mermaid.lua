-- Render with an atomic, validated cache. pandoc.pipe propagates renderer errors.
local filter_dir = pandoc.path.directory(PANDOC_SCRIPT_FILE)
local renderer = pandoc.path.join({filter_dir, "..", "scripts", "render-mermaid.py"})

function CodeBlock(block)
    if not block.classes:includes("mermaid") then return nil end
    local image = pandoc.pipe(os.getenv("PYTHON") or "python3", {renderer}, block.text)
    return pandoc.Para({pandoc.Image({}, image:gsub("%s+$", ""))})
end

function Para(para)
    local text = pandoc.utils.stringify(para)
    if text:match("^Previous:") or text:match("^Next:") or text:match("^Back to:") then
        return {}
    end
end
