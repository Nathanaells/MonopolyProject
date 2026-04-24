import { TopHatPiece, CarPiece, ScottieDogPiece, BattleshipPiece, HorsePiece, ThimblePiece, CannonPiece, WheelbarrowPiece } from "../Components/MonopolyPieces";
import type { PieceSVGProps } from "../Interfaces/Interface";


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
