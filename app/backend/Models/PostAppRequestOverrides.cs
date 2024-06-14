namespace MinimalApi.Models;
    public class PostAppRequestOverrides
    {
        public List<string> knowledge_base_category { get; set; } = [];
        public int? top {  get; set; }
        public string? retrieval_mode { get; set; }
        public bool? semantic_ranker {  get; set; }
        public bool? semantic_captions { get; set; }
        public bool? suggest_followup_questions { get; set; }
        public bool? use_oid_security_filter {  get; set; }
        public bool? use_groups_security_filter { get; set; }
        public List<string> vector_fields {  get; set; } = [];
        public bool use_gpt4v { get; set; }
        public string? gpt4v_input { get; set; }
        /**
        public string? exclude_category { get; set; }
        public double? temperature { get; set; }
        public string? prompt_template { get; set; }
        public string? prompt_template_prefix { get; set; }
        public string? prompt_template_suffix { get; set; }

        **/
    }
