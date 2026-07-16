#!/usr/bin/env python3
import argparse
import collections
import os
import sys

import torch
from safetensors.torch import serialize_file


def rename_key(rename_map, name):
    for old_value, new_value in rename_map.items():
        if old_value in name:
            name = name.replace(old_value, new_value)
    return name


def detect_version(loaded):
    version = 4.0
    for key in loaded.keys():
        if "ln_x" in key:
            version = max(5.0, version)
        if "gate.weight" in key:
            version = max(5.1, version)
        if int(version) == 5 and "att.time_decay" in key:
            value = loaded[key]
            if len(value.shape) > 1 and value.shape[1] > 1:
                version = max(5.2, version)
        if "time_maa" in key:
            version = max(6.0, version)
    return version


def convert_file(pt_filename, sf_filename, rename_map, transpose_names, force_dtype):
    loaded = torch.load(pt_filename, map_location="cpu")
    if "state_dict" in loaded:
        loaded = loaded["state_dict"]
    elif "model" in loaded and isinstance(loaded["model"], dict):
        loaded = loaded["model"]

    if not isinstance(loaded, collections.OrderedDict) and not isinstance(loaded, dict):
        raise TypeError(f"Unsupported checkpoint root type: {type(loaded)!r}")

    version = detect_version(loaded)
    print(f"[INFO] Model detected: v{version:.1f}")

    key_list = list(loaded.keys())
    if version == 5.1:
        _, n_emb = loaded["emb.weight"].shape
        for key in key_list:
            if "time_decay" in key or "time_faaaa" in key:
                loaded[key] = loaded[key].unsqueeze(1).repeat(1, n_emb // loaded[key].shape[0])

    converted = {}
    with torch.no_grad():
        for key in key_list:
            value = loaded[key]
            if not isinstance(value, torch.Tensor):
                continue

            new_key = rename_key(rename_map, key).lower()
            tensor = value.detach().contiguous()

            if force_dtype == "float16":
                tensor = tensor.half()
            elif force_dtype == "bfloat16":
                tensor = tensor.bfloat16()
            elif force_dtype == "float32":
                tensor = tensor.float()
            else:
                tensor = tensor.half()

            for transpose_name in transpose_names:
                if transpose_name in new_key:
                    dims = len(tensor.shape)
                    tensor = tensor.transpose(dims - 2, dims - 1).contiguous()
                    break

            converted[new_key] = {
                "dtype": str(tensor.dtype).split(".")[-1],
                "shape": list(tensor.shape),
                "data": tensor.numpy().tobytes(),
            }

    output_dir = os.path.dirname(sf_filename)
    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
    serialize_file(converted, sf_filename, metadata={"format": "pt"})
    print(f"[INFO] Converted tensors: {len(converted)}")
    print(f"[INFO] Saved to {sf_filename}")


def main():
    parser = argparse.ArgumentParser(description="Convert RWKV .pth checkpoint to official rwkv_lightning safetensors format.")
    parser.add_argument("--input", required=True, help="Path to input .pth model")
    parser.add_argument("--output", required=True, help="Path to output .st model")
    parser.add_argument(
        "--dtype",
        choices=["float16", "bfloat16", "float32", "auto"],
        default="float16",
        help="Tensor dtype for converted weights. Default uses float16, matching the official script's half() cast.")
    args = parser.parse_args()

    input_path = os.path.abspath(args.input)
    output_path = os.path.abspath(args.output)
    if not os.path.exists(input_path):
        print(f"[ERROR] Input file does not exist: {input_path}", file=sys.stderr)
        return 1

    print(f"[INFO] Loading checkpoint: {input_path}")
    convert_file(
        input_path,
        output_path,
        rename_map={
            "time_faaaa": "time_first",
            "time_maa": "time_mix",
            "lora_A": "lora.0",
            "lora_B": "lora.1",
        },
        transpose_names=[
            "time_mix_w1",
            "time_mix_w2",
            "time_decay_w1",
            "time_decay_w2",
            "w1",
            "w2",
            "a1",
            "a2",
            "g1",
            "g2",
            "v1",
            "v2",
            "time_state",
            "lora.0",
        ],
        force_dtype="float16" if args.dtype == "auto" else args.dtype,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
