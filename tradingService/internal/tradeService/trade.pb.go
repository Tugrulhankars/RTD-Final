
package tradeService

import (
	protoreflect "google.golang.org/protobuf/reflect/protoreflect"
	protoimpl "google.golang.org/protobuf/runtime/protoimpl"
	reflect "reflect"
	sync "sync"
	unsafe "unsafe"
)

const (
	_ = protoimpl.EnforceVersion(20 - protoimpl.MinVersion)
	_ = protoimpl.EnforceVersion(protoimpl.MaxVersion - 20)
)

type TradeType int32

const (
	TradeType_TRADE_TYPE_UNSPECIFIED TradeType = 0
	TradeType_BUY                    TradeType = 1
	TradeType_SELL                   TradeType = 2
)

var (
	TradeType_name = map[int32]string{
		0: "TRADE_TYPE_UNSPECIFIED",
		1: "BUY",
		2: "SELL",
	}
	TradeType_value = map[string]int32{
		"TRADE_TYPE_UNSPECIFIED": 0,
		"BUY":                    1,
		"SELL":                   2,
	}
)

func (x TradeType) Enum() *TradeType {
	p := new(TradeType)
	*p = x
	return p
}

func (x TradeType) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (TradeType) Descriptor() protoreflect.EnumDescriptor {
	return file_protos_trade_proto_enumTypes[0].Descriptor()
}

func (TradeType) Type() protoreflect.EnumType {
	return &file_protos_trade_proto_enumTypes[0]
}

func (x TradeType) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

func (TradeType) EnumDescriptor() ([]byte, []int) {
	return file_protos_trade_proto_rawDescGZIP(), []int{0}
}

type CreateTradeRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	AccountId     int32                  `protobuf:"varint,1,opt,name=account_id,json=accountId,proto3" json:"account_id,omitempty"`
	Symbol        string                 `protobuf:"bytes,2,opt,name=symbol,proto3" json:"symbol,omitempty"`
	Quantity      float32                `protobuf:"fixed32,3,opt,name=quantity,proto3" json:"quantity,omitempty"`
	Price         float32                `protobuf:"fixed32,4,opt,name=price,proto3" json:"price,omitempty"`
	Type          TradeType              `protobuf:"varint,5,opt,name=type,proto3,enum=tradeService.TradeType" json:"type,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CreateTradeRequest) Reset() {
	*x = CreateTradeRequest{}
	mi := &file_protos_trade_proto_msgTypes[0]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CreateTradeRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CreateTradeRequest) ProtoMessage() {}

func (x *CreateTradeRequest) ProtoReflect() protoreflect.Message {
	mi := &file_protos_trade_proto_msgTypes[0]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CreateTradeRequest) Descriptor() ([]byte, []int) {
	return file_protos_trade_proto_rawDescGZIP(), []int{0}
}

func (x *CreateTradeRequest) GetAccountId() int32 {
	if x != nil {
		return x.AccountId
	}
	return 0
}

func (x *CreateTradeRequest) GetSymbol() string {
	if x != nil {
		return x.Symbol
	}
	return ""
}

func (x *CreateTradeRequest) GetQuantity() float32 {
	if x != nil {
		return x.Quantity
	}
	return 0
}

func (x *CreateTradeRequest) GetPrice() float32 {
	if x != nil {
		return x.Price
	}
	return 0
}

func (x *CreateTradeRequest) GetType() TradeType {
	if x != nil {
		return x.Type
	}
	return TradeType_TRADE_TYPE_UNSPECIFIED
}

type CreateTradeResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Message       string                 `protobuf:"bytes,1,opt,name=message,proto3" json:"message,omitempty"`
	TradeId       int32                  `protobuf:"varint,2,opt,name=trade_id,json=tradeId,proto3" json:"trade_id,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CreateTradeResponse) Reset() {
	*x = CreateTradeResponse{}
	mi := &file_protos_trade_proto_msgTypes[1]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CreateTradeResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CreateTradeResponse) ProtoMessage() {}

func (x *CreateTradeResponse) ProtoReflect() protoreflect.Message {
	mi := &file_protos_trade_proto_msgTypes[1]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CreateTradeResponse) Descriptor() ([]byte, []int) {
	return file_protos_trade_proto_rawDescGZIP(), []int{1}
}

func (x *CreateTradeResponse) GetMessage() string {
	if x != nil {
		return x.Message
	}
	return ""
}

func (x *CreateTradeResponse) GetTradeId() int32 {
	if x != nil {
		return x.TradeId
	}
	return 0
}

var File_protos_trade_proto protoreflect.FileDescriptor

const file_protos_trade_proto_rawDesc = "" +
	"\n" +
	"\x12protos/trade.proto\x12\ftradeService\"\xaa\x01\n" +
	"\x12CreateTradeRequest\x12\x1d\n" +
	"\n" +
	"account_id\x18\x01 \x01(\x05R\taccountId\x12\x16\n" +
	"\x06symbol\x18\x02 \x01(\tR\x06symbol\x12\x1a\n" +
	"\bquantity\x18\x03 \x01(\x02R\bquantity\x12\x14\n" +
	"\x05price\x18\x04 \x01(\x02R\x05price\x12+\n" +
	"\x04type\x18\x05 \x01(\x0e2\x17.tradeService.TradeTypeR\x04type\"J\n" +
	"\x13CreateTradeResponse\x12\x18\n" +
	"\amessage\x18\x01 \x01(\tR\amessage\x12\x19\n" +
	"\btrade_id\x18\x02 \x01(\x05R\atradeId*:\n" +
	"\tTradeType\x12\x1a\n" +
	"\x16TRADE_TYPE_UNSPECIFIED\x10\x00\x12\a\n" +
	"\x03BUY\x10\x01\x12\b\n" +
	"\x04SELL\x10\x022b\n" +
	"\fTradeService\x12R\n" +
	"\vCreateTrade\x12 .tradeService.CreateTradeRequest\x1a!.tradeService.CreateTradeResponseB\x0fZ\r/tradeServiceb\x06proto3"

var (
	file_protos_trade_proto_rawDescOnce sync.Once
	file_protos_trade_proto_rawDescData []byte
)

func file_protos_trade_proto_rawDescGZIP() []byte {
	file_protos_trade_proto_rawDescOnce.Do(func() {
		file_protos_trade_proto_rawDescData = protoimpl.X.CompressGZIP(unsafe.Slice(unsafe.StringData(file_protos_trade_proto_rawDesc), len(file_protos_trade_proto_rawDesc)))
	})
	return file_protos_trade_proto_rawDescData
}

var file_protos_trade_proto_enumTypes = make([]protoimpl.EnumInfo, 1)
var file_protos_trade_proto_msgTypes = make([]protoimpl.MessageInfo, 2)
var file_protos_trade_proto_goTypes = []any{
	(TradeType)(0),
	(*CreateTradeRequest)(nil),
	(*CreateTradeResponse)(nil),
}
var file_protos_trade_proto_depIdxs = []int32{
	0,
	1,
	2,
	2,
	1,
	1,
	1,
	0,
}

func init() { file_protos_trade_proto_init() }
func file_protos_trade_proto_init() {
	if File_protos_trade_proto != nil {
		return
	}
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_protos_trade_proto_rawDesc), len(file_protos_trade_proto_rawDesc)),
			NumEnums:      1,
			NumMessages:   2,
			NumExtensions: 0,
			NumServices:   1,
		},
		GoTypes:           file_protos_trade_proto_goTypes,
		DependencyIndexes: file_protos_trade_proto_depIdxs,
		EnumInfos:         file_protos_trade_proto_enumTypes,
		MessageInfos:      file_protos_trade_proto_msgTypes,
	}.Build()
	File_protos_trade_proto = out.File
	file_protos_trade_proto_goTypes = nil
	file_protos_trade_proto_depIdxs = nil
}
