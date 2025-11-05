variable "ai_hub_name" {
  description = "Name of the Azure AI Foundry hub (Cognitive Services account)"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region for the resources"
  type        = string
}

variable "sku_name" {
  description = "SKU for AI Foundry (S0 is standard)"
  type        = string
  default     = "S0"
}

variable "model_deployment_name" {
  description = "Name for the GPT-4o deployment"
  type        = string
  default     = "gpt-4o"
}

variable "model_name" {
  description = "AI model name"
  type        = string
  default     = "gpt-4o"
}

variable "model_version" {
  description = "AI model version"
  type        = string
  default     = "2024-08-06"
}

variable "model_capacity" {
  description = "Capacity units for the model (tokens per minute in thousands)"
  type        = number
  default     = 10
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
