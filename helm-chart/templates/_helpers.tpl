{{- define "artsy.name" -}}
{{- default .Release.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{- define "artsy.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- include "artsy.name" . | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}

{{- define "artsy.labels" -}}
app.kubernetes.io/name: {{ include "artsy.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/version: {{ .Chart.AppVersion }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end }}

{{- define "artsy.selectorLabels" -}}
app.kubernetes.io/name: {{ include "artsy.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}