#version 330

uniform vec3 Ka, Kd, Ks;
uniform vec3 Ia, Il;
uniform float P;
uniform float K1, K2;
uniform vec3 LightPos, WatcherPos;

varying vec3 position;
varying vec3 normal;
varying vec3 color;

vec3 bound(vec3 v) {
    return min(max(v, 0.0), 1.0);
}

void main() {
    vec3 i = Ia * Ka;
    float d = length(LightPos - position);
    vec3 L = normalize(LightPos - position);
    vec3 R = normalize(reflect(-L, normal));
    vec3 S = normalize(WatcherPos - position);
    float cosRS = dot(R, S);
    float LN = dot(L, normal);
    if (LN > 0) {
        i += Il * Kd * LN / (d * K1 + K2);
    }
    if (cosRS > 0) {
        i += Il * Ks * pow(cosRS, P) / (d * K1 + K2);
    }
    gl_FragColor = vec4(bound(color * i), 1.0);
}