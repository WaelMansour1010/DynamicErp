import json

def main():
    log_path = r"C:\Users\Wael\.gemini\antigravity\brain\5adb7f99-5ea3-4a8d-b10a-b18350ccde2c\.system_generated\logs\transcript.jsonl"
    with open(log_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    print(f"Total lines in transcript: {len(lines)}")
    
    for line in lines:
        try:
            data = json.loads(line)
            step = data.get("step_index", 0)
            
            # Print steps between 290 and 555
            if 290 <= step <= 555:
                source = data.get("source", "UNKNOWN")
                type_name = data.get("type", "UNKNOWN")
                content = data.get("content", "")
                
                # We want user inputs and models plans/responses, or tool calls/outputs that are interesting
                if type_name in ["USER_INPUT", "PLANNER_RESPONSE"] or (source == "SYSTEM" and "finished with result" in content):
                    print(f"\n==========================================")
                    print(f"STEP {step} | Source: {source} | Type: {type_name}")
                    print(f"==========================================")
                    print(content[:1500])
        except Exception as e:
            pass

if __name__ == "__main__":
    main()
