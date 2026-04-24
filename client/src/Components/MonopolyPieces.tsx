import React from "react";
import type {PieceSVGProps} from "../Interfaces/Interface"

export const TopHatPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <rect x="6" y="34" width="36" height="5" rx="2" fill={color} />
    <rect x="14" y="12" width="20" height="24" rx="3" fill={color} />
    <rect x="11" y="30" width="26" height="5" rx="2" fill={color} />

    <rect
      x="14"
      y="27"
      width="20"
      height="4"
      rx="1"
      fill="white"
      opacity="0.25"
    />
  </svg>
);

export const CarPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M4 28 Q6 20 14 20 L22 16 L34 16 Q42 20 44 28 Z" fill={color} />
    <path d="M22 17 L22 21 L34 21 L32 17 Z" fill="white" opacity="0.3" />
    <circle cx="13" cy="31" r="5" fill="#222" />
    <circle cx="13" cy="31" r="2.5" fill="#555" />
    <circle cx="35" cy="31" r="5" fill="#222" />
    <circle cx="35" cy="31" r="2.5" fill="#555" />
    <rect x="6" y="26" width="36" height="5" rx="2" fill={color} />
  </svg>
);

export const ScottieDogPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <ellipse cx="22" cy="30" rx="14" ry="9" fill={color} />

    <ellipse cx="36" cy="22" rx="8" ry="7" fill={color} />
    <ellipse cx="42" cy="26" rx="4" ry="3" fill={color} />

    <path d="M32 16 L36 10 L40 16 Z" fill={color} />

    <circle cx="38" cy="21" r="1.5" fill="white" />
    <circle cx="38" cy="21" r="0.7" fill="#111" />

    <ellipse cx="44" cy="26" rx="2" ry="1.5" fill="#111" />

    <path
      d="M8 26 Q2 18 6 14"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
      fill="none"
    />

    <rect x="14" y="37" width="4" height="7" rx="2" fill={color} />
    <rect x="20" y="37" width="4" height="7" rx="2" fill={color} />
    <rect x="26" y="37" width="4" height="7" rx="2" fill={color} />
  </svg>
);

export const BattleshipPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M4 34 L6 28 L42 28 L44 34 Q24 40 4 34 Z" fill={color} />

    <rect x="10" y="22" width="28" height="7" rx="1" fill={color} />

    <rect x="16" y="16" width="16" height="7" rx="1" fill={color} />
    <rect x="20" y="12" width="8" height="5" rx="1" fill={color} />

    <rect x="30" y="20" width="14" height="3" rx="1.5" fill={color} />
    <rect x="8" y="24" width="8" height="2" rx="1" fill={color} />
    <circle cx="20" cy="19" r="1.5" fill="white" opacity="0.4" />
    <circle cx="26" cy="19" r="1.5" fill="white" opacity="0.4" />
  </svg>
);

export const HorsePiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <ellipse cx="24" cy="30" rx="13" ry="8" fill={color} />

    <path d="M30 25 Q34 16 32 12 Q28 10 26 14 L28 22 Z" fill={color} />

    <ellipse cx="32" cy="11" rx="5" ry="4" fill={color} />

    <path d="M30 8 L32 4 L34 8 Z" fill={color} />

    <path
      d="M32 15 Q36 18 34 22"
      stroke={color}
      strokeWidth="3"
      fill="none"
      strokeLinecap="round"
    />

    <circle cx="22" cy="20" r="4" fill={color} />
    <path d="M18 24 Q18 32 22 32 Q26 32 26 24 Z" fill={color} />

    <path
      d="M12 36 L10 44"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <path
      d="M18 37 L16 44"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <path
      d="M30 37 L32 44"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <path
      d="M36 35 L38 44"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <path
      d="M11 28 Q4 24 6 18"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
      fill="none"
    />
  </svg>
);

export const ThimblePiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M12 36 Q12 10 24 8 Q36 10 36 36 Z" fill={color} />

    <rect x="10" y="34" width="28" height="5" rx="2.5" fill={color} />

    <circle cx="20" cy="18" r="1" fill="white" opacity="0.3" />
    <circle cx="24" cy="14" r="1" fill="white" opacity="0.3" />
    <circle cx="28" cy="18" r="1" fill="white" opacity="0.3" />
    <circle cx="20" cy="24" r="1" fill="white" opacity="0.3" />
    <circle cx="24" cy="20" r="1" fill="white" opacity="0.3" />
    <circle cx="28" cy="24" r="1" fill="white" opacity="0.3" />
    <circle cx="20" cy="30" r="1" fill="white" opacity="0.3" />
    <circle cx="24" cy="26" r="1" fill="white" opacity="0.3" />
    <circle cx="28" cy="30" r="1" fill="white" opacity="0.3" />
  </svg>
);

export const CannonPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <rect x="10" y="18" width="30" height="10" rx="5" fill={color} />
    <rect x="36" y="20" width="8" height="6" rx="3" fill={color} />
    <rect
      x="8"
      y="19"
      width="5"
      height="8"
      rx="2.5"
      fill={color}
      opacity="0.7"
    />

    <path d="M12 28 L10 36 L20 36 L18 28 Z" fill={color} />
    <path d="M28 28 L26 36 L36 36 L34 28 Z" fill={color} />
    <rect x="14" y="34" width="20" height="3" rx="1.5" fill={color} />

    <circle cx="14" cy="38" r="6" fill={color} />
    <circle cx="14" cy="38" r="3" fill="white" opacity="0.2" />
    <circle cx="34" cy="38" r="6" fill={color} />
    <circle cx="34" cy="38" r="3" fill="white" opacity="0.2" />
    <circle cx="44" cy="20" r="3" fill={color} opacity="0.5" />
  </svg>
);

export const WheelbarrowPiece: React.FC<PieceSVGProps> = ({
  color = "currentColor",
  size = 48,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 48 48"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M14 16 L12 30 L34 30 L32 16 Z" fill={color} />

    <rect
      x="12"
      y="13"
      width="22"
      height="4"
      rx="2"
      fill={color}
      opacity="0.8"
    />

    <circle cx="10" cy="34" r="7" fill={color} />
    <circle cx="10" cy="34" r="3.5" fill="white" opacity="0.2" />

    <line
      x1="10"
      y1="27"
      x2="10"
      y2="41"
      stroke="white"
      strokeWidth="1.5"
      opacity="0.25"
    />
    <line
      x1="3"
      y1="34"
      x2="17"
      y2="34"
      stroke="white"
      strokeWidth="1.5"
      opacity="0.25"
    />

    <path
      d="M28 26 L44 20"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <path
      d="M30 30 L44 24"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />

    <line
      x1="18"
      y1="30"
      x2="16"
      y2="42"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
    <line
      x1="28"
      y1="30"
      x2="30"
      y2="42"
      stroke={color}
      strokeWidth="3"
      strokeLinecap="round"
    />
  </svg>
);


export const PieceComponents: Record<string, React.FC<PieceSVGProps>> = {
  Tophat: TopHatPiece,
  Car: CarPiece,
  ScottieDog: ScottieDogPiece,
  Battleship: BattleshipPiece,
  Horse: HorsePiece,
  Thimble: ThimblePiece,
  Cannon: CannonPiece,
  Wheelbarrow: WheelbarrowPiece,
};

export const PieceLabels: Record<string, string> = {
  Tophat: "Top Hat",
  Car: "Race Car",
  ScottieDog: "Scottie Dog",
  Battleship: "Battleship",
  Horse: "Horse & Rider",
  Thimble: "Thimble",
  Cannon: "Cannon",
  Wheelbarrow: "Wheelbarrow",
};

export const PieceOrder = [
  "Tophat",
  "Car",
  "ScottieDog",
  "Battleship",
  "Horse",
  "Thimble",
  "Cannon",
  "Wheelbarrow",
];
